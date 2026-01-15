using Microsoft.AspNetCore.Mvc;
using Ticketing.Core.Models;
using Ticketing.FrontOffice.Mvc.Services;
using Ticketing.FrontOffice.Mvc.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly CartService _cartService;
        private readonly DataAccessService _dataAccess;
        private readonly PapiPaymentService _paymentService;
        private readonly EmailService _emailService;
        private readonly TicketingDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            CartService cartService, 
            DataAccessService dataAccess, 
            PapiPaymentService paymentService,
            EmailService emailService,
            TicketingDbContext context,
            IConfiguration configuration,
            ILogger<CheckoutController> logger)
        {
            _cartService = cartService;
            _dataAccess = dataAccess;
            _paymentService = paymentService;
            _emailService = emailService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            return View(new CheckoutViewModel { TotalAmount = cart.TotalAmount });
        }

        [HttpPost]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            try
            {
                var mainReference = $"ORD-{DateTime.Now.Ticks}";
                _logger.LogInformation("Created payment reference: {Reference}", mainReference);

                var reservations = new List<Reservation>();

                var groupedItems = cart.Items
                    .GroupBy(item => new { item.EventId, item.TicketTypeId })
                    .ToList();

                _logger.LogInformation("Creating {Count} reservation(s) for reference: {Reference}", groupedItems.Count, mainReference);

                foreach (var group in groupedItems)
                {
                    var itemsInGroup = group.ToList();
                    var firstItem = itemsInGroup.First();
                    
                    var allSeats = itemsInGroup.SelectMany(item => item.Seats).ToList();
                    var totalQuantity = itemsInGroup.Sum(item => item.Quantity);
                    var totalAmount = itemsInGroup.Sum(item => item.Total);

                    var reservation = new Reservation
                    {
                        CustomerName = model.FullName,
                        Email = model.Email,
                        EventId = firstItem.EventId,
                        ReservationDate = DateTime.UtcNow,
                        SeatCount = allSeats.Any() ? allSeats.Count : totalQuantity,
                        Status = ReservationStatus.Pending,
                        TotalAmount = totalAmount,
                        PhoneNumber = model.PhoneNumber ?? "N/A",
                        PaymentMethod = model.PaymentMethod,
                        PaymentReference = mainReference // Shared reference for this entire checkout
                    };

                    foreach (var seat in allSeats)
                    {
                        var rowLetter = (char)('A' + seat.Row - 1);
                        reservation.Seats.Add(new Seat
                        {
                            PosX = seat.Row,
                            PosY = seat.Col,
                            Code = $"{rowLetter}{seat.Col}",
                            Status = SeatStatus.Reserved,
                            TicketTypeId = firstItem.TicketTypeId
                        });
                    }
                    
                    var reservationId = await _dataAccess.CreateReservationAsync(reservation);
                    reservation.Id = reservationId;
                    reservations.Add(reservation);
                    _logger.LogInformation("Created reservation {ReservationId} for reference: {Reference}", reservationId, mainReference);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("All {Count} reservations saved to database for reference: {Reference}", reservations.Count, mainReference);

                var simulatePayment = _configuration.GetValue<bool>("PapiSettings:SimulatePayment", false);
                _logger.LogInformation("Payment simulation mode: {SimulatePayment}", simulatePayment);
                
                if (simulatePayment)
                {
                    _logger.LogInformation("SIMULATION MODE: Automatically marking reservations as paid for reference: {Reference} (NO PAPI CALL)", mainReference);
                    
                    foreach (var res in reservations)
                    {
                        await _dataAccess.UpdateReservationPaymentAsync(
                            res.Id, 
                            ReservationStatus.Confirmed, 
                            mainReference, 
                            $"SIM-{DateTime.Now.Ticks}");
                        
                        var seats = await _context.Seats
                            .Where(s => s.ReservationId == res.Id)
                            .Include(s => s.TicketType)
                            .ToListAsync();
                        
                        foreach (var seat in seats)
                        {
                            if (seat.Status == SeatStatus.Reserved)
                            {
                                seat.Status = SeatStatus.Taken;
                            }
                        }
                        
                        var eventDetails = await _context.Events
                            .Include(e => e.Venue)
                            .FirstOrDefaultAsync(e => e.Id == res.EventId);
                        
                        if (eventDetails != null)
                        {
                            var reservationForEmail = new Reservation
                            {
                                Id = res.Id,
                                CustomerName = res.CustomerName,
                                Email = res.Email,
                                PhoneNumber = res.PhoneNumber,
                                SeatCount = res.SeatCount,
                                TotalAmount = res.TotalAmount,
                                ReservationDate = res.ReservationDate,
                                PaymentReference = res.PaymentReference,
                                PaymentMethod = res.PaymentMethod,
                                Seats = seats
                            };
                            
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _emailService.SendReservationConfirmationEmailAsync(reservationForEmail, eventDetails);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send confirmation email for reservation {Id}", res.Id);
                                }
                            });
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    _cartService.Clear();
                    
                    _logger.LogInformation("SIMULATION MODE: Payment completed successfully. Redirecting to success page.");
                    
                    return RedirectToAction("Success", "Payment", new { reference = mainReference });
                }
                else
                {
                    _logger.LogInformation("REAL PAYMENT MODE: Calling Papi API for reference: {Reference}", mainReference);
                    
                    string? provider = model.PaymentMethod switch
                    {
                        "MVOLA" => "MVOLA",
                        "ORANGE_MONEY" => "ORANGE_MONEY",
                        "AIRTEL_MONEY" => "AIRTEL_MONEY",
                        "BRED" => "BRED",
                        _ => null
                    };

                    _logger.LogInformation("Preparing Papi payment request - Amount: {Amount}, Provider: {Provider}, Reference: {Reference}", 
                        cart.TotalAmount, provider ?? "AUTO", mainReference);

                    var paymentRequest = _paymentService.PrepareRequest(
                        amount: cart.TotalAmount,
                        clientName: model.FullName,
                        email: model.Email,
                        reference: mainReference,
                        description: $" Test - test",
                        phoneNumber: model.PhoneNumber,
                        provider: provider,
                        httpContext: HttpContext
                    );
                    
                    _logger.LogInformation("Payment request prepared. Calling Papi API...");
                    _logger.LogDebug("Payment request details: {PaymentRequest}", JsonSerializer.Serialize(paymentRequest));
                    
                    var paymentResponse = await _paymentService.CreatePaymentLinkAsync(paymentRequest);

                    if (paymentResponse != null && !string.IsNullOrEmpty(paymentResponse.PaymentLink))
                    {
                        _logger.LogInformation("✅ Payment link created successfully!");
                        _logger.LogInformation("PaymentLink: {PaymentLink}", paymentResponse.PaymentLink);
                        _logger.LogInformation("PaymentReference: {PaymentReference}", paymentResponse.PaymentReference ?? mainReference);
                        _logger.LogInformation("NotificationToken: {Token}", paymentResponse.NotificationToken);

                        foreach (var res in reservations)
                        {
                            await _dataAccess.UpdateReservationPaymentAsync(
                                res.Id,
                                ReservationStatus.Pending,
                                paymentResponse.PaymentReference ?? mainReference,
                                paymentResponse.NotificationToken);

                            _logger.LogInformation("Updated reservation {ReservationId} with payment reference {PaymentReference} and notification token. Status: Pending (waiting for payment confirmation).",
                                res.Id, paymentResponse.PaymentReference ?? mainReference);
                        }

                        await _context.SaveChangesAsync();
                        _cartService.Clear();
                        _logger.LogInformation("Cart cleared. All reservations updated in database.");
                        
                        if (!Uri.IsWellFormedUriString(paymentResponse.PaymentLink, UriKind.Absolute))
                        {
                            _logger.LogError("❌ Invalid payment link URL: {PaymentLink}", paymentResponse.PaymentLink);
                            throw new Exception("Invalid payment link received from Papi. Please contact support.");
                        }

                        _logger.LogInformation("🚀 REDIRECTING user to Papi payment gateway: {PaymentLink}", paymentResponse.PaymentLink);
                        
                        return Redirect(paymentResponse.PaymentLink);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to create payment link. PaymentResponse is null or PaymentLink is empty.");
                        _logger.LogError("This could be due to:");
                        _logger.LogError("1. Papi API returned an error");
                        _logger.LogError("2. Network connectivity issues");
                        _logger.LogError("3. Invalid API configuration");
                        throw new Exception("Could not generate payment link from Papi. Please try again or contact support.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout for email: {Email}", model.Email);
                ModelState.AddModelError("", $"An error occurred while processing your payment: {ex.Message}. Please try again or contact support.");
                return View("Index", model);
            }
        }
    }
}

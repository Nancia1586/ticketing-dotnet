using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;
using Ticketing.FrontOffice.Mvc.Models;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DataAccessService _dataAccess;
        private readonly TicketingDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly EmailService _emailService;

        public PaymentController(DataAccessService dataAccess, TicketingDbContext context, ILogger<PaymentController> logger, EmailService emailService)
        {
            _dataAccess = dataAccess;
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<IActionResult> Success(string reference)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Event)
                .Include(r => r.Seats)
                .Where(r => r.PaymentReference == reference)
                .ToListAsync();

            if (!reservations.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // Group by ReservationId for display
            return View(reservations);
        }

        public IActionResult Failure()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Notify([FromBody] PapiNotificationPayload payload)
        {
            if (payload == null)
            {
                _logger.LogWarning("Received null notification payload");
                return BadRequest("Invalid payload");
            }

            _logger.LogInformation("Received Papi Notification - Status: {Status}, Reference: {Reference}, Amount: {Amount}", 
                payload.PaymentStatus, payload.PaymentReference, payload.Amount);

            // According to Papi documentation: Verify paymentReference and notificationToken
            // Step 1: Verify paymentReference matches what we sent
            if (string.IsNullOrEmpty(payload.PaymentReference))
            {
                _logger.LogWarning("Payment reference is missing in notification");
                return BadRequest(new { message = "Payment reference is required" });
            }

            // Step 2: Find reservations with this reference
            var reservations = await _context.Reservations
                .Where(r => r.PaymentReference == payload.PaymentReference)
                .ToListAsync();

            if (!reservations.Any())
            {
                _logger.LogWarning("No reservations found for Papi Reference {Reference}", payload.PaymentReference);
                return NotFound(new { message = "No reservations found for this reference" });
            }

            // Step 3: Verify notificationToken matches what we received when creating the payment link
            // This is critical for security - both must match for the notification to be genuine
            foreach (var reservation in reservations)
            {
                if (string.IsNullOrEmpty(reservation.NotificationToken) || 
                    reservation.NotificationToken != payload.NotificationToken)
                {
                    _logger.LogWarning("Notification Token Mismatch for Reservation {Id}. Received: {Received}, Expected: {Expected}", 
                        reservation.Id, payload.NotificationToken, reservation.NotificationToken ?? "NULL");
                    return BadRequest(new { message = "Token mismatch - notification is not genuine" });
                }
            }

            _logger.LogInformation("Notification verified successfully - paymentReference and notificationToken match");

            // 4. Update Status based on payment status
            var newStatus = payload.PaymentStatus.ToUpper() switch
            {
                "SUCCESS" => ReservationStatus.Confirmed,
                "FAILED" => ReservationStatus.Cancelled,
                "PENDING" => ReservationStatus.Pending,
                _ => ReservationStatus.Pending
            };

            foreach (var reservation in reservations)
            {
                var oldStatus = reservation.Status;
                reservation.Status = newStatus;
                
                _logger.LogInformation("Updating Reservation {Id} from {OldStatus} to {NewStatus}", 
                    reservation.Id, oldStatus, newStatus);
                
                // If SUCCESS, ensure seats are marked as TAKEN if they were RESERVED
                if (newStatus == ReservationStatus.Confirmed)
                {
                    var seats = await _context.Seats
                        .Where(s => s.ReservationId == reservation.Id)
                        .Include(s => s.TicketType)
                        .ToListAsync();
                    
                    foreach (var seat in seats)
                    {
                        if (seat.Status == SeatStatus.Reserved)
                        {
                            seat.Status = SeatStatus.Taken;
                            _logger.LogInformation("Marking seat {SeatCode} (Id: {SeatId}) as TAKEN", seat.Code, seat.Id);
                        }
                    }

                    // Load event details for email (after saving changes)
                    var eventDetails = await _context.Events
                        .Include(e => e.Venue)
                        .FirstOrDefaultAsync(e => e.Id == reservation.EventId);

                    if (eventDetails != null)
                    {
                        // Create a copy of reservation with seats for email
                        var reservationForEmail = new Reservation
                        {
                            Id = reservation.Id,
                            CustomerName = reservation.CustomerName,
                            Email = reservation.Email,
                            PhoneNumber = reservation.PhoneNumber,
                            SeatCount = reservation.SeatCount,
                            TotalAmount = reservation.TotalAmount,
                            ReservationDate = reservation.ReservationDate,
                            PaymentReference = reservation.PaymentReference,
                            PaymentMethod = reservation.PaymentMethod,
                            Seats = seats
                        };
                        
                        // Send confirmation email asynchronously (don't block the response)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendReservationConfirmationEmailAsync(reservationForEmail, eventDetails);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending email for reservation {ReservationId}", reservation.Id);
                            }
                        });
                    }
                }
                // If FAILED, release the seats back to FREE
                else if (newStatus == ReservationStatus.Cancelled)
                {
                    var seats = await _context.Seats
                        .Where(s => s.ReservationId == reservation.Id)
                        .ToListAsync();
                    
                    foreach (var seat in seats)
                    {
                        if (seat.Status == SeatStatus.Reserved)
                        {
                            seat.Status = SeatStatus.Free;
                            seat.ReservationId = null;
                            _logger.LogInformation("Releasing seat {SeatCode} (Id: {SeatId}) back to FREE", seat.Code, seat.Id);
                        }
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully processed notification for reference {Reference}. Status: {Status}", 
                    payload.PaymentReference, payload.PaymentStatus);
                
                return Ok(new { message = "Notification processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes for notification {Reference}", payload.PaymentReference);
                return StatusCode(500, new { message = "Error processing notification" });
            }
        }
    }
}

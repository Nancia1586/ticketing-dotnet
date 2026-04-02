using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

            return View(reservations);
        }

        public async Task<IActionResult> DownloadTickets(string reference)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Event).ThenInclude(e => e.Venue)
                .Include(r => r.Seats).ThenInclude(s => s.TicketType)
                .Where(r => r.PaymentReference == reference)
                .ToListAsync();

            if (!reservations.Any())
                return RedirectToAction("Index", "Home");

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                foreach (var reservation in reservations)
                {
                    foreach (var seat in reservation.Seats)
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A6.Landscape());
                            page.Margin(20);
                            page.DefaultTextStyle(s => s.FontSize(10).FontFamily("Arial"));

                            page.Content().Column(col =>
                            {
                                // Header
                                col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(8).Row(row =>
                                {
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text(reservation.Event.Name)
                                            .FontSize(16).Bold().FontColor(Colors.Black);
                                        inner.Item().Text(reservation.Event.Date.ToString("dd/MM/yyyy HH:mm"))
                                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                                        if (reservation.Event.Venue != null)
                                            inner.Item().Text(reservation.Event.Venue.Name)
                                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                                    });
                                    row.AutoItem().AlignRight().Column(inner =>
                                    {
                                        inner.Item().Text("BILLET").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                                        inner.Item().Text($"#{reservation.Id}").FontSize(9).FontColor(Colors.Grey.Medium);
                                    });
                                });

                                // Seat & type
                                col.Item().PaddingVertical(10).Row(row =>
                                {
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("PLACE").FontSize(7).FontColor(Colors.Grey.Medium);
                                        inner.Item().Text(seat.Code).FontSize(22).Bold();
                                    });
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("TYPE").FontSize(7).FontColor(Colors.Grey.Medium);
                                        inner.Item().Text(seat.TicketType?.Name ?? "-").FontSize(13).Bold();
                                        inner.Item().Text($"{seat.TicketType?.Price.ToString("N0") ?? "0"} Ar")
                                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                                    });
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("CLIENT").FontSize(7).FontColor(Colors.Grey.Medium);
                                        inner.Item().Text(reservation.CustomerName).FontSize(10).Bold();
                                    });
                                });

                                // Footer
                                col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6)
                                    .Text($"Réf : {reservation.PaymentReference}")
                                    .FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    }
                }
            }).GeneratePdf();

            var filename = $"billets-{reference}.pdf";
            return File(pdf, "application/pdf", filename);
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

            if (string.IsNullOrEmpty(payload.PaymentReference))
            {
                _logger.LogWarning("Payment reference is missing in notification");
                return BadRequest(new { message = "Payment reference is required" });
            }

            var reservations = await _context.Reservations
                .Where(r => r.PaymentReference == payload.PaymentReference)
                .ToListAsync();

            if (!reservations.Any())
            {
                _logger.LogWarning("No reservations found for Papi Reference {Reference}", payload.PaymentReference);
                return NotFound(new { message = "No reservations found for this reference" });
            }

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

                    var eventDetails = await _context.Events
                        .Include(e => e.Venue)
                        .FirstOrDefaultAsync(e => e.Id == reservation.EventId);
                    
                    if (eventDetails != null)
                    {
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

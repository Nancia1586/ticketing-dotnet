using System.Net;
using System.Net.Mail;
using System.Text;
using Ticketing.Core.Models;

namespace Ticketing.FrontOffice.Mvc.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendReservationConfirmationEmailAsync(Reservation reservation, Event eventDetails)
        {
            try
            {
                var emailBody = BuildReservationEmailBody(reservation, eventDetails);
                var subject = $"Confirmation de réservation - {eventDetails.Name}";

                await SendEmailAsync(reservation.Email, subject, emailBody);
                
                _logger.LogInformation("Confirmation email sent to {Email} for reservation {ReservationId}", 
                    reservation.Email, reservation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation email to {Email} for reservation {ReservationId}", 
                    reservation.Email, reservation.Id);
                // Don't throw - email failure shouldn't break the payment flow
            }
        }

        private string BuildReservationEmailBody(Reservation reservation, Event eventDetails)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }");
            sb.AppendLine(".content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }");
            sb.AppendLine(".section { background: white; padding: 20px; margin: 20px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine(".section-title { color: #667eea; font-size: 18px; font-weight: bold; margin-bottom: 15px; border-bottom: 2px solid #667eea; padding-bottom: 10px; }");
            sb.AppendLine(".info-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; }");
            sb.AppendLine(".info-label { font-weight: bold; color: #666; }");
            sb.AppendLine(".info-value { color: #333; }");
            sb.AppendLine(".seat-list { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 10px; }");
            sb.AppendLine(".seat-badge { background: #667eea; color: white; padding: 8px 15px; border-radius: 20px; font-weight: bold; }");
            sb.AppendLine(".total { background: #667eea; color: white; padding: 20px; border-radius: 8px; text-align: center; margin-top: 20px; }");
            sb.AppendLine(".total-amount { font-size: 28px; font-weight: bold; margin: 10px 0; }");
            sb.AppendLine(".footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");
            
            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>✓ Réservation Confirmée</h1>");
            sb.AppendLine("<p>Merci pour votre achat !</p>");
            sb.AppendLine("</div>");
            
            // Content
            sb.AppendLine("<div class='content'>");
            
            // Event Information
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>📅 Informations sur l'événement</div>");
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Événement:</span><span class='info-value'>{WebUtility.HtmlEncode(eventDetails.Name)}</span></div>");
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Date:</span><span class='info-value'>{eventDetails.Date:dd MMMM yyyy à HH:mm}</span></div>");
            if (eventDetails.Venue != null)
            {
                sb.AppendLine($"<div class='info-row'><span class='info-label'>Lieu:</span><span class='info-value'>{WebUtility.HtmlEncode(eventDetails.Venue.Name)}</span></div>");
                if (!string.IsNullOrEmpty(eventDetails.Venue.Address))
                {
                    sb.AppendLine($"<div class='info-row'><span class='info-label'>Adresse:</span><span class='info-value'>{WebUtility.HtmlEncode(eventDetails.Venue.Address)}</span></div>");
                }
            }
            sb.AppendLine("</div>");
            
            // Reservation Details
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>🎫 Détails de la réservation</div>");
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Référence:</span><span class='info-value'>{WebUtility.HtmlEncode(reservation.PaymentReference ?? $"RES-{reservation.Id}")}</span></div>");
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Date de réservation:</span><span class='info-value'>{reservation.ReservationDate:dd MMMM yyyy à HH:mm}</span></div>");
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Nom:</span><span class='info-value'>{WebUtility.HtmlEncode(reservation.CustomerName)}</span></div>");
            sb.AppendLine($"<div class='info-row'><span class='info-label'>Email:</span><span class='info-value'>{WebUtility.HtmlEncode(reservation.Email)}</span></div>");
            if (!string.IsNullOrEmpty(reservation.PhoneNumber) && reservation.PhoneNumber != "N/A")
            {
                sb.AppendLine($"<div class='info-row'><span class='info-label'>Téléphone:</span><span class='info-value'>{WebUtility.HtmlEncode(reservation.PhoneNumber)}</span></div>");
            }
            sb.AppendLine("</div>");
            
            // Seats Information
            if (reservation.Seats != null && reservation.Seats.Any())
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<div class='section-title'>🪑 Places réservées</div>");
                sb.AppendLine($"<div class='info-row'><span class='info-label'>Nombre de places:</span><span class='info-value'>{reservation.SeatCount}</span></div>");
                
                // Group seats by ticket type
                var seatsByType = reservation.Seats
                    .GroupBy(s => s.TicketType?.Name ?? "Standard")
                    .ToList();
                
                foreach (var group in seatsByType)
                {
                    var ticketType = group.First().TicketType;
                    var price = ticketType?.Price ?? 0;
                    var seats = group.Select(s => s.Code).OrderBy(c => c).ToList();
                    
                    sb.AppendLine($"<div style='margin: 15px 0; padding: 15px; background: #f0f0f0; border-radius: 5px;'>");
                    sb.AppendLine($"<strong>{WebUtility.HtmlEncode(group.Key)}</strong> - {price:N0} Ar par place");
                    sb.AppendLine("<div class='seat-list'>");
                    foreach (var seatCode in seats)
                    {
                        sb.AppendLine($"<span class='seat-badge'>{WebUtility.HtmlEncode(seatCode)}</span>");
                    }
                    sb.AppendLine("</div>");
                    sb.AppendLine($"<div style='margin-top: 10px; color: #666;'>Quantité: {seats.Count} × {price:N0} Ar = {(seats.Count * price):N0} Ar</div>");
                    sb.AppendLine("</div>");
                }
                
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<div class='section-title'>🎫 Billets</div>");
                sb.AppendLine($"<div class='info-row'><span class='info-label'>Nombre de billets:</span><span class='info-value'>{reservation.SeatCount}</span></div>");
                sb.AppendLine("<div style='margin-top: 10px; color: #666;'>Admission générale (pas de places assignées)</div>");
                sb.AppendLine("</div>");
            }
            
            // Total Amount
            sb.AppendLine("<div class='total'>");
            sb.AppendLine("<div>Montant total</div>");
            sb.AppendLine($"<div class='total-amount'>{reservation.TotalAmount:N0} Ar</div>");
            sb.AppendLine($"<div>Méthode de paiement: {WebUtility.HtmlEncode(reservation.PaymentMethod)}</div>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("</div>"); // content
            
            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Ceci est un email automatique, merci de ne pas y répondre.</p>");
            sb.AppendLine("<p>Pour toute question, contactez notre service client.</p>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("</div>"); // container
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUser;
            var fromName = _configuration["EmailSettings:FromName"] ?? "Ticketing System";

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings not configured. Email not sent to {To}", to);
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}


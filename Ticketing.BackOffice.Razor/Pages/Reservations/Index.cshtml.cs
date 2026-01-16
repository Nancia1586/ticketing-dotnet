using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.BackOffice.Razor.Extensions;
using System.Text;

namespace Ticketing.BackOffice.Razor.Pages.Reservations
{
    public class IndexModel : PageModel
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(IReservationRepository reservationRepository, UserManager<ApplicationUser> userManager)
        {
            _reservationRepository = reservationRepository;
            _userManager = userManager;
        }

        public IEnumerable<Reservation> Reservations { get; set; } = new List<Reservation>();
        public string? SearchTerm { get; set; }
        public int ReservationCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public async Task OnGetAsync(string? searchTerm = null, int page = 1)
        {
            SearchTerm = searchTerm;
            CurrentPage = page < 1 ? 1 : page;
            
            int? organizerId = null;
            ApplicationUser? currentUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUser = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Organizer"))
                {
                    organizerId = currentUser?.OrganizationId;
                }
            }

            // Pass user to layout
            ViewData["CurrentUser"] = currentUser;

            // SERVER-SIDE PAGINATION: Request only the current page from database
            // The repository executes Skip() and Take() in SQL, not in memory
            var result = await _reservationRepository.GetAllReservationsAsync(organizerId, searchTerm, CurrentPage, PageSize);
            
            // Validate page number doesn't exceed total pages
            if (result.TotalPages > 0 && CurrentPage > result.TotalPages)
            {
                CurrentPage = result.TotalPages;
                // Re-fetch with corrected page number
                result = await _reservationRepository.GetAllReservationsAsync(organizerId, searchTerm, CurrentPage, PageSize);
            }
            
            Reservations = result.Items;
            ReservationCount = result.TotalCount;
            TotalPages = result.TotalPages;
            HasPreviousPage = result.HasPreviousPage;
            HasNextPage = result.HasNextPage;
        }

        public async Task<IActionResult> OnGetExportCsvAsync(string? searchTerm = null)
        {
            int? organizerId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Organizer"))
                {
                    organizerId = currentUser?.OrganizationId;
                }
            }

            var reservations = await _reservationRepository.GetAllReservationsForExportAsync(organizerId, searchTerm);

            // Group reservations by event
            var groupedByEvent = reservations
                .GroupBy(r => new { r.Event.Id, r.Event.Name, r.Event.Date })
                .OrderBy(g => g.Key.Name)
                .ToList();

            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("Événement,Date Événement,Référence,Client,Email,Téléphone,Nombre de Places,Montant Total,Méthode de Paiement,Référence Paiement,Statut,Date Réservation");

            // Add reservations grouped by event
            foreach (var eventGroup in groupedByEvent)
            {
                var eventName = eventGroup.Key.Name;
                var eventDate = eventGroup.Key.Date.ToString("dd/MM/yyyy HH:mm");

                foreach (var reservation in eventGroup.OrderByDescending(r => r.ReservationDate))
                {
                    var line = new StringBuilder();
                    line.Append(EscapeCsvField(eventName));
                    line.Append(",");
                    line.Append(EscapeCsvField(eventDate));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.Reference ?? ""));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.CustomerName));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.Email));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.PhoneNumber));
                    line.Append(",");
                    line.Append(reservation.SeatCount);
                    line.Append(",");
                    line.Append(reservation.TotalAmount.ToString("F2"));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.PaymentMethod));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.PaymentReference ?? ""));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.Status.ToFrenchString()));
                    line.Append(",");
                    line.Append(EscapeCsvField(reservation.ReservationDate.ToString("dd/MM/yyyy HH:mm")));
                    
                    csv.AppendLine(line.ToString());
                }
            }

            var fileName = $"Reservations_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            // Add BOM for Excel compatibility
            var bomBytes = Encoding.UTF8.GetPreamble();
            var result = new byte[bomBytes.Length + bytes.Length];
            Buffer.BlockCopy(bomBytes, 0, result, 0, bomBytes.Length);
            Buffer.BlockCopy(bytes, 0, result, bomBytes.Length, bytes.Length);

            return File(result, "text/csv; charset=utf-8", fileName);
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}

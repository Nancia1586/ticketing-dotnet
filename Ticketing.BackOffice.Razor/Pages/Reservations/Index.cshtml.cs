using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

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
    }
}

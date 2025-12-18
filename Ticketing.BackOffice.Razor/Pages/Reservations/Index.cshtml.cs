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

        public async Task OnGetAsync()
        {
            int? organizerId = null;
            if (User.IsInRole("Organizer"))
            {
                var user = await _userManager.GetUserAsync(User);
                organizerId = user?.OrganizationId;
            }

            Reservations = await _reservationRepository.GetAllReservationsAsync(organizerId);
        }
    }
}

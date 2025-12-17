using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Pages.Reservations
{
    public class IndexModel : PageModel
    {
        private readonly IReservationRepository _reservationRepository;

        public IndexModel(IReservationRepository reservationRepository)
        {
            _reservationRepository = reservationRepository;
        }

        public IEnumerable<Reservation> Reservations { get; set; } = new List<Reservation>();

        public async Task OnGetAsync()
        {
            Reservations = await _reservationRepository.GetAllReservationsAsync();
        }
    }
}

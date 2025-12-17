using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Pages.Reservations
{
    public class DetailsModel : PageModel
    {
        private readonly IReservationRepository _reservationRepository;

        public DetailsModel(IReservationRepository reservationRepository)
        {
            _reservationRepository = reservationRepository;
        }

        public Reservation Reservation { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var res = await _reservationRepository.GetReservationByIdAsync(id);
            if (res == null)
            {
                return NotFound();
            }
            Reservation = res;
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, ReservationStatus status)
        {
            await _reservationRepository.UpdateReservationStatusAsync(id, status);
            return RedirectToPage(new { id });
        }
    }
}

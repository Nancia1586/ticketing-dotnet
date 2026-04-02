using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Pages.Reservations
{
    public class DetailsModel : PageModel
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(IReservationRepository reservationRepository, UserManager<ApplicationUser> userManager)
        {
            _reservationRepository = reservationRepository;
            _userManager = userManager;
        }

        public Reservation Reservation { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewData["CurrentUser"] = await _userManager.GetUserAsync(User);
            }

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
            TempData["SuccessMessage"] = "Statut de la commande mis à jour.";
            return RedirectToPage(new { id });
        }
    }
}

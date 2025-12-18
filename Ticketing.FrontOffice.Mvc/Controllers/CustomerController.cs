using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class CustomerController : Controller
    {
        private readonly DataAccessService _dataAccess;

        public CustomerController(DataAccessService dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task<IActionResult> MyReservations(string? email)
        {
            if (User.Identity.IsAuthenticated && string.IsNullOrEmpty(email))
            {
                email = User.Identity.Name;
            }

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View();
            }

            try
            {
                var reservations = await _dataAccess.GetReservationsByEmailAsync(email);

                ViewBag.Email = email;
                return View(reservations);
            }
            catch (Exception ex)
            {
                // Log error and return empty list
                ModelState.AddModelError("", "Unable to retrieve reservations. Please try again later.");
                ViewBag.Email = email;
                return View(new List<Ticketing.Core.Models.Reservation>());
            }
        }
    }
}

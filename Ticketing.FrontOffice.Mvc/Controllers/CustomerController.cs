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

        public IActionResult MyReservations()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MyReservations(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View();
            }

            var reservations = await _dataAccess.GetReservationsByEmailAsync(email);

            ViewBag.Email = email;
            return View(reservations);
        }
    }
}

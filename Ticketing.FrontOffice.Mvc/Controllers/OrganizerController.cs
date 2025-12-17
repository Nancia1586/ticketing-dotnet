using Microsoft.AspNetCore.Mvc;
using Ticketing.Core.Models;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly DataAccessService _dataAccess;

        public OrganizerController(DataAccessService dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Organizer organizer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // In real app: Check if email exists, Hash password
                    await _dataAccess.CreateOrganizerAsync(organizer);

                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    // Log error and show message
                    ModelState.AddModelError("", "Unable to create organizer account. Please try again later.");
                    return View(organizer);
                }
            }
            return View(organizer);
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}

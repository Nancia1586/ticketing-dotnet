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
                // In real app: Check if email exists, Hash password
                await _dataAccess.CreateOrganizerAsync(organizer);

                return RedirectToAction("Success");
            }
            return View(organizer);
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}

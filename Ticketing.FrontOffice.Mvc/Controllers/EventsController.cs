using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Models;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class EventsController : Controller
    {
        private readonly DataAccessService _dataAccess;

        public EventsController(DataAccessService dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task<IActionResult> Index(string? searchTerm, DateTime? filterDate)
        {
            var events = await _dataAccess.GetActiveEventsAsync(searchTerm, filterDate);

            var viewModel = new EventListViewModel
            {
                Events = events,
                SearchTerm = searchTerm,
                FilterDate = filterDate
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var evt = await _dataAccess.GetEventByIdAsync(id);

            if (evt == null)
            {
                return NotFound();
            }

            var viewModel = new EventDetailsViewModel
            {
                Event = evt
            };

            return View(viewModel);
        }
    }
}

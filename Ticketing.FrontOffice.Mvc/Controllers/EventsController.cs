using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Models;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class EventsController : Controller
    {
        private readonly DataAccessService _dataAccess;
        private readonly CartService _cartService;

        public EventsController(DataAccessService dataAccess, CartService cartService)
        {
            _dataAccess = dataAccess;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index(string? searchTerm, DateTime? filterDate)
        {
            try
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
            catch (Exception ex)
            {
                var viewModel = new EventListViewModel
                {
                    Events = new List<Ticketing.Core.Models.Event>(),
                    SearchTerm = searchTerm,
                    FilterDate = filterDate
                };
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return NotFound();
                }

                var evt = await _dataAccess.GetEventByIdAsync(id);

                if (evt == null)
                {
                    return NotFound();
                }

                var cart = _cartService.GetCart();
                var cartItemsForEvent = cart.Items.Where(i => i.EventId == id).ToList();

                var similarEvents = await _dataAccess.GetActiveEventsAsync();
                similarEvents = similarEvents
                    .Where(e => e.Id != id && (e.CategoryId == evt.CategoryId || e.CategoryId == 0))
                    .Take(4)
                    .ToList();

                var viewModel = new EventDetailsViewModel
                {
                    Event = evt,
                    CartItems = cartItemsForEvent,
                    SimilarEvents = similarEvents
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return NotFound();
            }
        }
    }
}

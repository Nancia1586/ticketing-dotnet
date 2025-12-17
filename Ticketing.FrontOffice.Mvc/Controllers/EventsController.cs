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
                // Log error but don't crash the application
                // Return empty list if database connection fails
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
                var evt = await _dataAccess.GetEventByIdAsync(id);

                if (evt == null)
                {
                    return NotFound();
                }

                // Get cart items for this event to show which seats are already in cart
                var cart = _cartService.GetCart();
                var cartItemsForEvent = cart.Items.Where(i => i.EventId == id).ToList();

                var viewModel = new EventDetailsViewModel
                {
                    Event = evt,
                    CartItems = cartItemsForEvent
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error and return not found
                return NotFound();
            }
        }
    }
}

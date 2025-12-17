using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Models;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly DataAccessService _dataAccess;

        public CartController(CartService cartService, DataAccessService dataAccess)
        {
            _cartService = cartService;
            _dataAccess = dataAccess;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int eventId, int ticketTypeId, int quantity, List<SeatSelection> seats)
        {
            try
            {
                var evt = await _dataAccess.GetEventByIdAsync(eventId);
                var ticketType = await _dataAccess.GetTicketTypeByIdAsync(ticketTypeId);

                if (evt == null || ticketType == null)
                {
                    return NotFound();
                }

            // Normalize seats list (remove null entries)
            var validSeats = seats?.Where(s => s != null && s.Row > 0 && s.Col > 0).ToList() ?? new List<SeatSelection>();

            // If seats are selected, override quantity with the actual number of seats
            if (validSeats.Any())
            {
                quantity = validSeats.Count;
            }
            else if (quantity <= 0)
            {
                quantity = 1; // Default to 1 if no seats and no quantity specified
            }

            var item = new CartItem
            {
                EventId = eventId,
                EventName = evt.Name,
                TicketTypeId = ticketTypeId,
                TicketTypeName = ticketType.Name,
                Price = ticketType.Price,
                Quantity = quantity,
                Seats = validSeats
            };

                _cartService.AddItem(item);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log error and redirect to events page
                TempData["Error"] = "Unable to add item to cart. Please try again.";
                return RedirectToAction("Index", "Events");
            }
        }

        public IActionResult Remove(Guid id)
        {
            _cartService.RemoveItem(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

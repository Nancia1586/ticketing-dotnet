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
            var evt = await _dataAccess.GetEventByIdAsync(eventId);
            var ticketType = await _dataAccess.GetTicketTypeByIdAsync(ticketTypeId);

            if (evt == null || ticketType == null)
            {
                return NotFound();
            }

            // If seats are selected, override quantity
            if (seats != null && seats.Any())
            {
                quantity = seats.Count;
            }

            var item = new CartItem
            {
                EventId = eventId,
                EventName = evt.Name,
                TicketTypeId = ticketTypeId,
                TicketTypeName = ticketType.Name,
                Price = ticketType.Price,
                Quantity = quantity,
                Seats = seats ?? new List<SeatSelection>()
            };

            _cartService.AddItem(item);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(Guid id)
        {
            _cartService.RemoveItem(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

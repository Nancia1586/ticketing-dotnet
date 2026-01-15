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
                if (evt == null) return NotFound();
                
                var validSeats = seats?.Where(s => s != null && s.Row > 0 && s.Col > 0).ToList() ?? new List<SeatSelection>();

                if (validSeats.Any())
                {
                    var seatsByTicketType = validSeats.GroupBy(s => s.TicketTypeId);

                    foreach (var group in seatsByTicketType)
                    {
                        var groupTicketTypeId = group.Key;
                        var groupSeats = group.ToList();
                        
                        var groupTicketType = await _dataAccess.GetTicketTypeByIdAsync(groupTicketTypeId);
                        if (groupTicketType == null) continue;

                        var item = new CartItem
                        {
                            EventId = eventId,
                            EventName = evt.Name,
                            TicketTypeId = groupTicketTypeId,
                            TicketTypeName = groupTicketType.Name,
                            Price = groupTicketType.Price,
                            Quantity = groupSeats.Count,
                            Seats = groupSeats
                        };
                        _cartService.AddItem(item);
                    }
                }
                else
                {
                    var ticketType = await _dataAccess.GetTicketTypeByIdAsync(ticketTypeId);
                    
                    if (ticketType == null) return NotFound();

                    if (quantity <= 0) quantity = 1;

                    var item = new CartItem
                    {
                        EventId = eventId,
                        EventName = evt.Name,
                        TicketTypeId = ticketTypeId,
                        TicketTypeName = ticketType.Name,
                        Price = ticketType.Price,
                        Quantity = quantity,
                        Seats = new List<SeatSelection>()
                    };
                    _cartService.AddItem(item);
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
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

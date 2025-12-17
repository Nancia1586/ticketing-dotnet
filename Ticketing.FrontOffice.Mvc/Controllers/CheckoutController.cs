using Microsoft.AspNetCore.Mvc;
using Ticketing.Core.Models;
using Ticketing.FrontOffice.Mvc.Services;
using Ticketing.FrontOffice.Mvc.Models;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly CartService _cartService;
        private readonly DataAccessService _dataAccess;

        public CheckoutController(CartService cartService, DataAccessService dataAccess)
        {
            _cartService = cartService;
            _dataAccess = dataAccess;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            return View(new CheckoutViewModel { TotalAmount = cart.TotalAmount });
        }

        [HttpPost]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            try
            {
                // Simulate Payment (always success)
                
                var reservations = new List<Reservation>();

            // Group cart items by EventId and TicketTypeId to create one reservation per group
            var groupedItems = cart.Items
                .GroupBy(item => new { item.EventId, item.TicketTypeId })
                .ToList();

            foreach (var group in groupedItems)
            {
                var itemsInGroup = group.ToList();
                var firstItem = itemsInGroup.First();
                
                // Collect all seats from all items in this group
                var allSeats = itemsInGroup.SelectMany(item => item.Seats).ToList();
                var totalQuantity = itemsInGroup.Sum(item => item.Quantity);
                var totalAmount = itemsInGroup.Sum(item => item.Total);

                var reservation = new Reservation
                {
                    CustomerName = model.FullName,
                    Email = model.Email,
                    EventId = firstItem.EventId,
                    ReservationDate = DateTime.UtcNow,
                    SeatCount = allSeats.Any() ? allSeats.Count : totalQuantity,
                    Status = ReservationStatus.Confirmed,
                    TotalAmount = totalAmount,
                    PhoneNumber = "N/A", // Optional in form
                    PaymentMethod = model.PaymentMethod
                };

                // Add all Seats from the group
                foreach (var seat in allSeats)
                {
                    var rowLetter = (char)('A' + seat.Row - 1);
                    reservation.Seats.Add(new Seat
                    {
                        PosX = seat.Row,
                        PosY = seat.Col,
                        Code = $"{rowLetter}{seat.Col}", // Format: A1, A2, B1, etc.
                        Status = SeatStatus.Reserved,
                        TicketTypeId = firstItem.TicketTypeId
                    });
                }
                
                // If generic tickets (no seats), we use the quantity count.

                var reservationId = await _dataAccess.CreateReservationAsync(reservation);
                reservation.Id = reservationId;
                
                // Load the full reservation with Event data for confirmation view
                var fullReservation = await _dataAccess.GetReservationByIdAsync(reservationId);
                if (fullReservation != null)
                {
                    reservations.Add(fullReservation);
                }
            }

            _cartService.Clear();

            // Load ticket types for all reservations to pass to view
            var ticketTypeCache = new Dictionary<int, TicketType>();
            foreach (var res in reservations)
            {
                foreach (var seat in res.Seats)
                {
                    if (!ticketTypeCache.ContainsKey(seat.TicketTypeId))
                    {
                        var ticketType = await _dataAccess.GetTicketTypeByIdAsync(seat.TicketTypeId);
                        if (ticketType != null)
                        {
                            ticketTypeCache[seat.TicketTypeId] = ticketType;
                        }
                    }
                }
            }
            
                ViewBag.TicketTypes = ticketTypeCache;

                return View("Confirmation", reservations);
            }
            catch (Exception ex)
            {
                // Log error and show error message
                ModelState.AddModelError("", "An error occurred while processing your order. Please try again.");
                return View("Index", model);
            }
        }
    }
}

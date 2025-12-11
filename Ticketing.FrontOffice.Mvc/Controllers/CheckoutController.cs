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

            // Simulate Payment (always success)
            
            var reservations = new List<Reservation>();

            foreach (var item in cart.Items)
            {
                var reservation = new Reservation
                {
                    CustomerName = model.FullName,
                    Email = model.Email,
                    EventId = item.EventId,
                    ReservationDate = DateTime.UtcNow,
                    SeatCount = item.Quantity,
                    Status = ReservationStatus.Confirmed,
                    TotalAmount = item.Total,
                    PhoneNumber = "N/A" // Optional in form
                };

                // Add Seats
                foreach (var seat in item.Seats)
                {
                    reservation.Seats.Add(new Seat
                    {
                        PosX = seat.Row,
                        PosY = seat.Col,
                        Code = $"R{seat.Row}-C{seat.Col}",
                        Status = SeatStatus.Reserved,
                        TicketTypeId = item.TicketTypeId // Store ticket type on seat too? Or just Reservation?
                    });
                }
                
                // If generic tickets (no seats), we might just stick with reservation count.
                // But for completeness if Seats are provided we use them.

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

            return View("Confirmation", reservations);
        }
    }
}

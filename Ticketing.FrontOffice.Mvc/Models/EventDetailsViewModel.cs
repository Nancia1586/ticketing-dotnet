using Ticketing.Core.Models;

namespace Ticketing.FrontOffice.Mvc.Models
{
    public class EventDetailsViewModel
    {
        public Event Event { get; set; } = null!;
        
        // For booking form
        public int SelectedTicketTypeId { get; set; }
        public int Quantity { get; set; } = 1;
        
        // Cart items for this event (to show which seats are in cart)
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}

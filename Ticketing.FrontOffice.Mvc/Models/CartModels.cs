namespace Ticketing.FrontOffice.Mvc.Models
{
    public class CartItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int TicketTypeId { get; set; }
        public string TicketTypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public List<SeatSelection> Seats { get; set; } = new List<SeatSelection>();

        public decimal Total => Price * Quantity;
    }

    public class SeatSelection
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal TotalAmount => Items.Sum(i => i.Total);
    }
}

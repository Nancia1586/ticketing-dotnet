namespace Ticketing.Core.Models
{
    public class ReservationDetail
    {
        public int Id { get; set; }
        
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;
        
        public int TicketTypeId { get; set; }
        public TicketType TicketType { get; set; } = null!;
        
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }
}

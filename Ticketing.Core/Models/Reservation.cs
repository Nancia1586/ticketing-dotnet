namespace Ticketing.Core.Models
{
    public enum ReservationStatus {
        Pending,
        Confirmed,
        Cancelled
    }

    public class Reservation
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;  
        public int SeatCount { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<ReservationDetail> ReservationDetails { get; set; } = new List<ReservationDetail>();

        public int EventId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Event Event { get; set; } = null!;
    }
}
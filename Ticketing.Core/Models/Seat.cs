namespace Ticketing.Core.Models
{
    public enum SeatStatus {
        Free,
        Held,
        Reserved
    }

    public class Seat
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; 
        public int PosX { get; set; }
        public int PosY { get; set; }
        public SeatStatus Status { get; set; } = SeatStatus.Free;
        
        public int TicketTypeId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public TicketType TicketType { get; set; } = null!;
        public int? ReservationId { get; set; }
        public Reservation? Reservation { get; set; }
    }
}
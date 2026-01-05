namespace Ticketing.Core.Models
{
    public class TicketType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TotalCapacity { get; set; } 
        public string Color { get; set; } = "#3b82f6";

        
        public int EventId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Event Event { get; set; } = null!;
        
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
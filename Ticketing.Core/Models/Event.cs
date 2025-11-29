namespace Ticketing.Core.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? PosterUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
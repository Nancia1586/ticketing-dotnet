using System.ComponentModel.DataAnnotations;

namespace Ticketing.Core.Models
{
    public class Venue
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }

        // JSON string to store layout configuration (e.g., non-assignable zones)
        // Format: Array of objects { r: row, c: col, type: 'stage'|'void'|... }
        public string LayoutJson { get; set; } = "[]";

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}

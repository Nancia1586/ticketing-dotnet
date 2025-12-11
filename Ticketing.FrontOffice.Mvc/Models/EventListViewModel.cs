using Ticketing.Core.Models;

namespace Ticketing.FrontOffice.Mvc.Models
{
    public class EventListViewModel
    {
        public IEnumerable<Event> Events { get; set; } = new List<Event>();
        
        // Filters
        public DateTime? FilterDate { get; set; }
        public string? SearchTerm { get; set; }
    }
}

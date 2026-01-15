using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class DetailsModel : PageModel
    {
        private readonly IEventService _eventService;

        public DetailsModel(IEventService eventService)
        {
            _eventService = eventService;
        }

        public Event Event { get; set; } = default!;
        
        public List<SalesSummaryDto> SalesSummary { get; set; } = new();
        public decimal TotalRevenue => SalesSummary.Sum(s => s.Revenue);
        public int TotalSold => SalesSummary.Sum(s => s.Sold);
        public int TotalCapacity => SalesSummary.Sum(s => s.Capacity);

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var evt = await _eventService.GetEventWithDetailsByIdAsync(id);
            if (evt == null)
            {
                return NotFound();
            }

            Event = evt;

            SalesSummary = Event.TicketTypes.Select(tt => new SalesSummaryDto
            {
                Name = tt.Name,
                Price = tt.Price,
                Capacity = tt.Seats.Count,
                Sold = tt.Seats.Count(s => s.Status != SeatStatus.Free),
                Color = tt.Color
            }).ToList();

            return Page();
        }
    }

    public class SalesSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public int Sold { get; set; }
        public decimal Revenue => Sold * Price;
        public string Color { get; set; } = "#3b82f6";
    }
}

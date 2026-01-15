using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class PlanModel : PageModel
    {
        private readonly IEventService _eventService; 

        public PlanModel(IEventService eventService)
        {
            _eventService = eventService;
        }
        
        public Event Event { get; set; } = new Event();

        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }

        [BindProperty]
        public List<TicketTypePlanDto> TicketTypePlans { get; set; } = new List<TicketTypePlanDto>();



        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue || id.Value == 0)
            {
                return RedirectToPage("./Index"); 
            }
            
            var loadedEvent = await _eventService.GetEventWithPlanByIdAsync(id.Value); 

            if (loadedEvent == null)
            {
                return NotFound();
            }

            Event = loadedEvent;
            
            TicketTypePlans = Event.TicketTypes.Select(tt => new TicketTypePlanDto
            {
                TicketTypeId = tt.Id,
                Name = tt.Name,
                Price = tt.Price,
                Color = tt.Color,

                SelectedSeatsJson = JsonSerializer.Serialize(tt.Seats.Select(s => s.Code).ToArray())
            }).ToList();

            if (Event.Venue != null)
            {
                TotalRows = Event.Venue.TotalRows;
                TotalColumns = Event.Venue.TotalColumns;
            }
            else
            {
                TotalRows = 0;
                TotalColumns = 0;
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int eventId)
        {
            var loadedEvent = await _eventService.GetEventWithPlanByIdAsync(eventId);
            if (loadedEvent == null) return NotFound();

            if (!ModelState.IsValid)
            {
                Event = loadedEvent;
                if (Event.Venue != null)
                {
                    TotalRows = Event.Venue.TotalRows;
                    TotalColumns = Event.Venue.TotalColumns;
                }
                return Page();
            }

            if (loadedEvent.IsSubmitted)
            {
                 return RedirectToPage("./Plan", new { id = eventId });
            }

            await _eventService.UpdateEventPlanAsync(eventId, TicketTypePlans);

            return RedirectToPage("./Plan", new { id = eventId });
        }

        public async Task<IActionResult> OnPostSubmitAsync(int eventId)
        {
            var loadedEvent = await _eventService.GetEventWithPlanByIdAsync(eventId);
            if (loadedEvent == null) return NotFound();

            if (!loadedEvent.IsSubmitted)
            {
                await _eventService.SubmitEventAsync(eventId);
            }

            return RedirectToPage("./Plan", new { id = eventId });
        }
    }
}
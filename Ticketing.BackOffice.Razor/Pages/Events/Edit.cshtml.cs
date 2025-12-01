using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class EditModel : PageModel
    {
        private readonly IEventService _eventService; 

        public EditModel(IEventService eventService)
        {
            _eventService = eventService;
        }

        [BindProperty]
        public Event Event { get; set; } = default!; 

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventData = await _eventService.GetEventByIdAsync(id.Value); 

            if (eventData == null)
            {
                return NotFound();
            }

            Event = eventData;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!string.IsNullOrEmpty(Event.PosterBase64))
            {
                Event.PosterUrl = Event.PosterBase64; 
                Event.PosterBase64 = null;
            }
            await _eventService.UpdateEventAsync(Event); 

            return RedirectToPage("./Index");
        }
    }
}
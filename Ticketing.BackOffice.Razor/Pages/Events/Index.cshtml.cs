using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class IndexModel : PageModel
    {
        private readonly IEventService _eventService;

        public IndexModel(IEventService eventService)
        {
            _eventService = eventService;
        }

        public IList<Event> Events { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Events = (await _eventService.GetAllEventsAsync()).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _eventService.DeleteEventAsync(id);
            return RedirectToPage();
        }
        
        public async Task<IActionResult> OnPostToggleStatusAsync(int id, bool isActive)
        {
            await _eventService.ToggleEventStatusAsync(id, isActive);
            return RedirectToPage();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class CreateModel : PageModel
    {
        private readonly IEventService _eventService;

        public CreateModel(IEventService eventService)
        {
            _eventService = eventService;
        }

        [BindProperty]
        public Event Event { get; set; } = default!;

        public IActionResult OnGet()
        {
            // Initialisation avec quelques valeurs par défaut
            Event = new Event { Date = DateTime.Now.AddDays(7), IsActive = true };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _eventService.CreateEventAsync(Event);

            return RedirectToPage("./Index");
        }
    }
}
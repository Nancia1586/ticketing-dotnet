using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class IndexModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(IEventService eventService, UserManager<ApplicationUser> userManager)
        {
            _eventService = eventService;
            _userManager = userManager;
        }

        public IList<Event> Events { get; set; } = default!;

        public async Task OnGetAsync()
        {
            int? organizerId = null;
            if (User.IsInRole("Organizer"))
            {
                var user = await _userManager.GetUserAsync(User);
                organizerId = user?.OrganizationId;
            }

            Events = (await _eventService.GetAllEventsAsync(organizerId)).ToList();
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
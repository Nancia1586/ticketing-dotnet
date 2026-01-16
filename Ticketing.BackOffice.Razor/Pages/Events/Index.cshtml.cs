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

        public IEnumerable<Event> Events { get; set; } = new List<Event>();
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public async Task OnGetAsync(string? searchTerm = null, int page = 1)
        {
            SearchTerm = searchTerm;
            CurrentPage = page < 1 ? 1 : page;

            int? organizerId = null;
            ApplicationUser? currentUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUser = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Organizer"))
                {
                    organizerId = currentUser?.OrganizationId;
                }
            }

            ViewData["CurrentUser"] = currentUser;

            var result = await _eventService.GetAllEventsAsync(organizerId, searchTerm, CurrentPage, PageSize);
            
            Events = result.Items;
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
            HasPreviousPage = result.HasPreviousPage;
            HasNextPage = result.HasNextPage;
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
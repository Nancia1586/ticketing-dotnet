using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class CreateModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly ICategoryService _categoryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(IEventService eventService, ICategoryService categoryService, UserManager<ApplicationUser> userManager)
        {
            _eventService = eventService;
            _categoryService = categoryService;
            _userManager = userManager;
        }

        [BindProperty]
        public Event Event { get; set; } = default!;

        public SelectList VenueSelectList { get; set; } = null!;
        public SelectList CategorySelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            Event = new Event { Date = DateTime.Now.AddDays(7), IsActive = true };
            var venues = await _eventService.GetAllVenuesAsync();
            VenueSelectList = new SelectList(venues, "Id", "Name");
            
            var categories = await _categoryService.GetAllCategoriesAsync();
            CategorySelectList = new SelectList(categories.Where(c => c.IsActive), "Id", "Name");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var venues = await _eventService.GetAllVenuesAsync();
                VenueSelectList = new SelectList(venues, "Id", "Name");
                
                var categories = await _categoryService.GetAllCategoriesAsync();
                CategorySelectList = new SelectList(categories.Where(c => c.IsActive), "Id", "Name");
                return Page();
            }
            
            if (!string.IsNullOrEmpty(Event.PosterBase64))
            {
                Event.PosterUrl = Event.PosterBase64; 
                
                Event.PosterBase64 = null; 
            }
            
            if (User.IsInRole("Organizer"))
            {
                var user = await _userManager.GetUserAsync(User);
                Event.OrganizerId = user?.OrganizationId;
            }
            
            await _eventService.CreateEventAsync(Event);

            return RedirectToPage("./Index");
        }
    }
}
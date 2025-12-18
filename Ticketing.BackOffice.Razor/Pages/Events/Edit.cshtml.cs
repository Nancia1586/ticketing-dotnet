using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Pages.Events
{
    public class EditModel : PageModel
    {
        private readonly IEventService _eventService;
        private readonly ICategoryService _categoryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(IEventService eventService, ICategoryService categoryService, UserManager<ApplicationUser> userManager)
        {
            _eventService = eventService;
            _categoryService = categoryService;
            _userManager = userManager;
        }

        [BindProperty]
        public Event Event { get; set; } = default!; 

        public SelectList VenueSelectList { get; set; }
        public SelectList CategorySelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventData = await _eventService.GetEventByIdAsync(id.Value); 

            if (User.IsInRole("Organizer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (eventData.OrganizerId != user?.OrganizationId)
                {
                    return Forbid();
                }
            }

            Event = eventData;
            
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
                if (Event.OrganizerId != user?.OrganizationId)
                {
                    return Forbid();
                }
            }

            await _eventService.UpdateEventAsync(Event); 

            return RedirectToPage("./Index");
        }
    }
}
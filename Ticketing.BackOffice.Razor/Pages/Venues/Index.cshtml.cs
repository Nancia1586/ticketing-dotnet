using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Ticketing.Core.Data;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Venues
{
    public class IndexModel : PageModel
    {
        private readonly TicketingDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(TicketingDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Venue> Venues { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewData["CurrentUser"] = await _userManager.GetUserAsync(User);
            }
            Venues = await _context.Venues.AsNoTracking().ToListAsync();
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Venues
{
    public class IndexModel : PageModel
    {
        private readonly TicketingDbContext _context;

        public IndexModel(TicketingDbContext context)
        {
            _context = context;
        }

        public IList<Venue> Venues { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Venues = await _context.Venues.ToListAsync();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Pages.Venues
{
    public class EditModel : PageModel
    {
        private readonly TicketingDbContext _context;

        public EditModel(TicketingDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Venue Venue { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var venue = await _context.Venues.FirstOrDefaultAsync(m => m.Id == id);
            if (venue == null)
            {
                return NotFound();
            }
            Venue = venue;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Venue).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VenueExists(Venue.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.Id == id);
        }
    }
}

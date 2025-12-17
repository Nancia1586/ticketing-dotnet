using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public class VenueService : IVenueService
    {
        private readonly TicketingDbContext _context;

        public VenueService(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
            return await _context.Venues.ToListAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Data
{
    public class TicketingDbContext : DbContext
    {
        public TicketingDbContext(DbContextOptions<TicketingDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Spectacle> Spectacles { get; set; }
    }
}
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
        
        public DbSet<Event> Events { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Venue> Venues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Seat>()
                .HasOne(s => s.Reservation)
                .WithMany(r => r.Seats)
                .HasForeignKey(s => s.ReservationId)
                .IsRequired(false); 

            modelBuilder.Entity<TicketType>()
                .Property(tt => tt.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalAmount)
                .HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Models;

namespace Ticketing.Core.Data
{
    public class TicketingDbContext : IdentityDbContext<ApplicationUser>
    {
        public TicketingDbContext(DbContextOptions<TicketingDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Event> Events { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationDetail> ReservationDetails { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Organizer> Organizers { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // Configure max lengths for searchable columns to allow indexing
            modelBuilder.Entity<Reservation>()
                .Property(r => r.Reference)
                .HasMaxLength(100);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.CustomerName)
                .HasMaxLength(200);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.Email)
                .HasMaxLength(255);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.PaymentReference)
                .HasMaxLength(100);

            // Create indexes for better search performance
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.Reference)
                .HasDatabaseName("IX_Reservations_Reference");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.CustomerName)
                .HasDatabaseName("IX_Reservations_CustomerName");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.Email)
                .HasDatabaseName("IX_Reservations_Email");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.PaymentReference)
                .HasDatabaseName("IX_Reservations_PaymentReference");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.Status)
                .HasDatabaseName("IX_Reservations_Status");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.EventId, r.Status })
                .HasDatabaseName("IX_Reservations_EventId_Status");

            modelBuilder.Entity<ReservationDetail>()
                .Property(rd => rd.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ReservationDetail>()
                .HasOne(rd => rd.TicketType)
                .WithMany()
                .HasForeignKey(rd => rd.TicketTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Seat Code with max length for indexing
            modelBuilder.Entity<Seat>()
                .Property(s => s.Code)
                .HasMaxLength(50);

            // Configure Seat indexes for better performance on large datasets
            modelBuilder.Entity<Seat>()
                .HasIndex(s => s.Status)
                .HasDatabaseName("IX_Seats_Status");

            modelBuilder.Entity<Seat>()
                .HasIndex(s => s.ReservationId)
                .HasDatabaseName("IX_Seats_ReservationId");

            modelBuilder.Entity<Seat>()
                .HasIndex(s => s.TicketTypeId)
                .HasDatabaseName("IX_Seats_TicketTypeId");

            modelBuilder.Entity<Seat>()
                .HasIndex(s => new { s.TicketTypeId, s.Status })
                .HasDatabaseName("IX_Seats_TicketTypeId_Status");

            // Index on Code for search performance
            modelBuilder.Entity<Seat>()
                .HasIndex(s => s.Code)
                .HasDatabaseName("IX_Seats_Code");
        }
    }
}

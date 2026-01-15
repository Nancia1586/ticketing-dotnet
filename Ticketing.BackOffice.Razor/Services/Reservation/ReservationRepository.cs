using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly TicketingDbContext _context;

        public ReservationRepository(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync(int? organizerId = null, string? searchTerm = null)
        {
            var query = _context.Reservations
                .Include(r => r.Event)
                .Include(r => r.Seats)
                .AsQueryable();

            if (organizerId.HasValue)
            {
                query = query.Where(r => r.Event.OrganizerId == organizerId);
            }

            // Search functionality
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(r => 
                    r.Reference.Contains(searchTerm) ||
                    r.CustomerName.Contains(searchTerm) ||
                    r.Email.Contains(searchTerm) ||
                    r.Event.Name.Contains(searchTerm) ||
                    (r.PaymentReference != null && r.PaymentReference.Contains(searchTerm))
                );
            }

            return await query.OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                .Include(r => r.Seats)
                .ThenInclude(s => s.TicketType)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task UpdateReservationStatusAsync(int id, ReservationStatus status)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                reservation.Status = status;
                await _context.SaveChangesAsync();
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;

namespace Ticketing.FrontOffice.Mvc.Services
{
    public class DataAccessService
    {
        private readonly TicketingDbContext _context;

        public DataAccessService(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetActiveEventsAsync(string? searchTerm, DateTime? filterDate)
        {
            var query = _context.Events
                .Include(e => e.Venue)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Seats)
                .Include(e => e.Reservations)
                    .ThenInclude(r => r.Seats)
                .Where(e => e.IsActive && e.Date >= DateTime.Now)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => 
                    e.Name.Contains(searchTerm) || 
                    e.Description.Contains(searchTerm) ||
                    (e.Venue != null && e.Venue.Name.Contains(searchTerm)));
            }

            if (filterDate.HasValue)
            {
                query = query.Where(e => e.Date.Date == filterDate.Value.Date);
            }

            return await query
                .OrderBy(e => e.Date)
                .ToListAsync();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Seats)
                .Include(e => e.Reservations)
                    .ThenInclude(r => r.Seats)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<TicketType?> GetTicketTypeByIdAsync(int id)
        {
            return await _context.TicketTypes
                .Include(tt => tt.Event)
                .FirstOrDefaultAsync(tt => tt.Id == id);
        }

        public async Task<int> CreateReservationAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation.Id;
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                    .ThenInclude(e => e.Venue)
                .Include(r => r.Seats)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Reservation>> GetReservationsByEmailAsync(string email)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                    .ThenInclude(e => e.Venue)
                .Include(r => r.Seats)
                .Where(r => r.Email == email)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task CreateOrganizerAsync(Organizer organizer)
        {
            _context.Organizers.Add(organizer);
            await _context.SaveChangesAsync();
        }
    }
}


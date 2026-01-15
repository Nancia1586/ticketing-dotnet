using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Services
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly TicketingDbContext _context;

        public ReservationRepository(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Reservation>> GetAllReservationsAsync(int? organizerId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 10)
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit max page size for performance

            // Build the base query with Event only (lightweight - no seats yet)
            // This query is executed SERVER-SIDE in SQL
            var query = _context.Reservations
                .Include(r => r.Event)
                .AsNoTracking()
                .AsQueryable();

            if (organizerId.HasValue)
            {
                query = query.Where(r => r.Event.OrganizerId == organizerId);
            }

            // Search functionality (executed SERVER-SIDE)
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

            // SERVER-SIDE: Get total count before pagination (executed in SQL)
            // This counts all matching records without loading them into memory
            var totalCount = await query.CountAsync();

            // SERVER-SIDE PAGINATION: Only fetch the requested page from database
            // Skip() and Take() are translated to SQL OFFSET and FETCH NEXT
            // Only the records for the current page are loaded into memory
            var reservations = await query
                .OrderByDescending(r => r.ReservationDate)
                .Skip((pageNumber - 1) * pageSize)  // SQL: OFFSET
                .Take(pageSize)                      // SQL: FETCH NEXT
                .ToListAsync();

            // Load only first 3 seats per reservation - optimized approach
            if (reservations.Any())
            {
                var reservationIds = reservations.Select(r => r.Id).ToList();
                
                // OPTIMIZED: Load seats ordered by ReservationId and Id, then group in memory
                // This is more efficient than loading all seats when we only need 3 per reservation
                // The query is still filtered by reservationIds, so we only load relevant seats
                var allSeats = await _context.Seats
                    .Where(s => s.ReservationId.HasValue && reservationIds.Contains(s.ReservationId.Value))
                    .OrderBy(s => s.ReservationId)
                    .ThenBy(s => s.Id) // Consistent ordering per reservation
                    .AsNoTracking()
                    .ToListAsync();

                // Group by reservation and take only first 3 seats per reservation
                // This is done in memory but is efficient since we've already filtered by reservationIds
                // and the number of reservations per page is limited (max 100)
                var seatsByReservationId = allSeats
                    .GroupBy(s => s.ReservationId!.Value)
                    .ToDictionary(
                        g => g.Key, 
                        g => g.Take(3).ToList()
                    );

                // Assign seats to reservations (initialize empty list if no seats found)
                foreach (var reservation in reservations)
                {
                    reservation.Seats = seatsByReservationId.TryGetValue(reservation.Id, out var reservationSeats)
                        ? reservationSeats
                        : new List<Seat>();
                }
            }

            return new PagedResult<Reservation>
            {
                Items = reservations,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                .Include(r => r.Seats)
                    .ThenInclude(s => s.TicketType)
                .AsNoTracking()
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

        public async Task<DashboardStats> GetDashboardStatsAsync(int? organizerId = null)
        {
            // Build base query with Event included for organizer filter and event name
            var baseQuery = _context.Reservations
                .Include(r => r.Event)
                .AsNoTracking()
                .AsQueryable();
            
            if (organizerId.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.Event.OrganizerId == organizerId);
            }

            // Get total reservations count (executed in SQL)
            var totalReservations = await baseQuery.CountAsync();

            // Get confirmed reservations stats using SQL aggregation (much faster than loading all data)
            var confirmedQuery = baseQuery.Where(r => r.Status == ReservationStatus.Confirmed);
            
            var confirmedStats = await confirmedQuery
                .GroupBy(r => 1) // Group all records together for aggregation
                .Select(g => new
                {
                    TotalTicketsSold = g.Sum(r => r.SeatCount),
                    TotalRevenue = g.Sum(r => r.TotalAmount)
                })
                .FirstOrDefaultAsync();

            // Get top 5 events by tickets sold using SQL aggregation
            var topEventsQuery = confirmedQuery
                .GroupBy(r => new { r.EventId, r.Event.Name })
                .Select(g => new TopEventStats
                {
                    EventId = g.Key.EventId,
                    EventName = g.Key.Name,
                    TicketsSold = g.Sum(r => r.SeatCount),
                    Revenue = g.Sum(r => r.TotalAmount)
                })
                .OrderByDescending(e => e.TicketsSold)
                .Take(5);

            var topEvents = await topEventsQuery.ToListAsync();

            return new DashboardStats
            {
                TotalReservations = totalReservations,
                TotalTicketsSold = confirmedStats?.TotalTicketsSold ?? 0,
                TotalRevenue = confirmedStats?.TotalRevenue ?? 0,
                TopEvents = topEvents
            };
        }
    }
}

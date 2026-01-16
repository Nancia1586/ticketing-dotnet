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

            // OPTIMIZED: Use join with projection instead of Include for better performance
            var query = from reservation in _context.Reservations
                       join evt in _context.Events on reservation.EventId equals evt.Id
                       select new
                       {
                           Reservation = reservation,
                           Event = evt
                       };

            // Apply filters
            if (organizerId.HasValue)
            {
                query = query.Where(x => x.Event.OrganizerId == organizerId);
            }

            // Search functionality (executed SERVER-SIDE)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(x => 
                    x.Reservation.Reference.Contains(searchTerm) ||
                    x.Reservation.CustomerName.Contains(searchTerm) ||
                    x.Reservation.Email.Contains(searchTerm) ||
                    x.Event.Name.Contains(searchTerm) ||
                    (x.Reservation.PaymentReference != null && x.Reservation.PaymentReference.Contains(searchTerm))
                );
            }

            // SERVER-SIDE: Get total count before pagination (executed in SQL)
            var totalCount = await query.CountAsync();

            // OPTIMIZED: Project directly to Reservation with Event data in a single query
            var reservationData = await query
                .OrderByDescending(x => x.Reservation.ReservationDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    Reservation = x.Reservation,
                    EventId = x.Event.Id,
                    EventName = x.Event.Name
                })
                .AsNoTracking()
                .ToListAsync();

            // Get reservation IDs before processing
            var reservationIds = reservationData.Select(x => x.Reservation.Id).ToList();

            // OPTIMIZED: Load seats in parallel with reservation processing
            var seatsTask = reservationIds.Any() 
                ? _context.Seats
                    .Where(s => s.ReservationId.HasValue && reservationIds.Contains(s.ReservationId.Value))
                    .OrderBy(s => s.ReservationId)
                    .ThenBy(s => s.Id)
                    .AsNoTracking()
                    .ToListAsync()
                : Task.FromResult(new List<Seat>());

            // Extract reservations and assign Event navigation property
            var reservations = reservationData.Select(x => 
            {
                var res = x.Reservation;
                // Set Event navigation property with minimal data (only what's needed for display)
                res.Event = new Event 
                { 
                    Id = x.EventId, 
                    Name = x.EventName
                };
                return res;
            }).ToList();

            // Wait for seats to load
            var allSeats = await seatsTask;

            // Group seats by reservation and take only first 3 seats per reservation
            if (allSeats.Any())
            {
                var seatsByReservationId = allSeats
                    .GroupBy(s => s.ReservationId!.Value)
                    .ToDictionary(
                        g => g.Key, 
                        g => g.Take(3).ToList()
                    );

                // Assign seats to reservations
                foreach (var reservation in reservations)
                {
                    reservation.Seats = seatsByReservationId.TryGetValue(reservation.Id, out var reservationSeats)
                        ? reservationSeats
                        : new List<Seat>();
                }
            }
            else
            {
                // Initialize empty seats list for all reservations
                foreach (var reservation in reservations)
                {
                    reservation.Seats = new List<Seat>();
                }
            }

            // Order reservations to match the original query order
            var orderedReservations = reservationIds
                .Select(id => reservations.First(r => r.Id == id))
                .ToList();

            return new PagedResult<Reservation>
            {
                Items = orderedReservations,
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
            // OPTIMIZED: Use join instead of Include for better performance
            var baseQuery = from reservation in _context.Reservations
                           join evt in _context.Events on reservation.EventId equals evt.Id
                           select new
                           {
                               Reservation = reservation,
                               Event = evt
                           };
            
            if (organizerId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Event.OrganizerId == organizerId);
            }

            // Get total reservations count (executed in SQL)
            var totalReservations = await baseQuery.CountAsync();

            // Get confirmed reservations stats using SQL aggregation (much faster than loading all data)
            var confirmedQuery = baseQuery.Where(x => x.Reservation.Status == ReservationStatus.Confirmed);
            
            var confirmedStats = await confirmedQuery
                .GroupBy(x => 1) // Group all records together for aggregation
                .Select(g => new
                {
                    TotalTicketsSold = g.Sum(x => x.Reservation.SeatCount),
                    TotalRevenue = g.Sum(x => x.Reservation.TotalAmount)
                })
                .FirstOrDefaultAsync();

            // Get top 5 events by tickets sold using SQL aggregation
            var topEventsQuery = confirmedQuery
                .GroupBy(x => new { x.Event.Id, x.Event.Name })
                .Select(g => new TopEventStats
                {
                    EventId = g.Key.Id,
                    EventName = g.Key.Name,
                    TicketsSold = g.Sum(x => x.Reservation.SeatCount),
                    Revenue = g.Sum(x => x.Reservation.TotalAmount)
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

using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;
using System.Text.Json;

namespace Ticketing.BackOffice.Razor.Services
{
    public class EventRepository : IEventRepository
    {
        private readonly TicketingDbContext _context;

        public EventRepository(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Event>> GetAllEventsAsync(int? organizerId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            // Base query — no Include, projection handles joins
            var baseQuery = _context.Events.AsNoTracking().AsQueryable();

            if (organizerId.HasValue)
                baseQuery = baseQuery.Where(e => e.OrganizerId == organizerId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                baseQuery = baseQuery.Where(e =>
                    e.Name.Contains(searchTerm) ||
                    (e.Venue != null && e.Venue.Name.Contains(searchTerm)) ||
                    (e.Category != null && e.Category.Name.Contains(searchTerm)));
            }

            var totalCount = await baseQuery.CountAsync();

            // Project only columns needed for the list — excludes PosterBase64, Description, TicketTypes, Seats, Reservations
            var items = await baseQuery
                .OrderByDescending(e => e.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new Event
                {
                    Id          = e.Id,
                    Name        = e.Name,
                    Date        = e.Date,
                    IsActive    = e.IsActive,
                    IsSubmitted = e.IsSubmitted,
                    PosterUrl   = e.PosterUrl,
                    OrganizerId = e.OrganizerId,
                    Venue    = e.Venue    != null ? new Venue    { Id = e.Venue.Id,    Name = e.Venue.Name }    : null,
                    Category = e.Category != null ? new Category { Id = e.Category.Id, Name = e.Category.Name } : null
                })
                .ToListAsync();

            return new PagedResult<Event>
            {
                Items      = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        public async Task UpdateEventAsync(Event eventToUpdate)
        {
            var existing = await _context.Events.FindAsync(eventToUpdate.Id);
            if (existing == null) return;

            existing.Name        = eventToUpdate.Name;
            existing.Description = eventToUpdate.Description;
            existing.VenueId     = eventToUpdate.VenueId;
            existing.CategoryId  = eventToUpdate.CategoryId;
            existing.Date        = eventToUpdate.Date;
            existing.IsActive    = eventToUpdate.IsActive;
            existing.IsSubmitted = eventToUpdate.IsSubmitted || eventToUpdate.IsActive;
            existing.OrganizerId = eventToUpdate.OrganizerId;

            if (!string.IsNullOrEmpty(eventToUpdate.PosterUrl))
                existing.PosterUrl = eventToUpdate.PosterUrl;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteEventAsync(int id)
        {
            var eventToDelete = await _context.Events.FindAsync(id);
            if (eventToDelete != null)
            {
                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Event?> GetEventWithPlanByIdAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.Seats)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event?> GetEventWithDetailsByIdAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.Seats)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans)
        {
            var eventToUpdate = await GetEventWithPlanByIdAsync(eventId);
            if (eventToUpdate == null) return;

            var inputTypeIds = ticketTypePlans.Where(p => p.TicketTypeId != 0).Select(p => p.TicketTypeId).ToList();
            
            var typesToRemove = eventToUpdate.TicketTypes.Where(tt => !inputTypeIds.Contains(tt.Id)).ToList();
            foreach (var type in typesToRemove)
            {
                _context.Remove(type); 
            }

            foreach (var planInput in ticketTypePlans)
            {
                TicketType? ticketType;

                if (planInput.TicketTypeId == 0)
                {
                    ticketType = new TicketType
                    {
                        EventId = eventId,
                        Name = planInput.Name,
                        Price = planInput.Price,
                        Color = planInput.Color
                    };
                    eventToUpdate.TicketTypes.Add(ticketType);
                }
                else
                {
                    ticketType = eventToUpdate.TicketTypes.FirstOrDefault(tt => tt.Id == planInput.TicketTypeId);
                    if (ticketType != null)
                    {
                        ticketType.Name = planInput.Name;
                        ticketType.Price = planInput.Price;
                        ticketType.Color = planInput.Color;
                    }
                    else
                    {
                        continue; 
                    }
                }

                var selectedSeats = new HashSet<string>();
                try 
                {
                    if (!string.IsNullOrEmpty(planInput.SelectedSeatsJson))
                    {
                        var seats = JsonSerializer.Deserialize<string[]>(planInput.SelectedSeatsJson);
                        if (seats != null)
                        {
                            foreach(var s in seats) selectedSeats.Add(s);
                        }
                    }
                }
                catch { }

                var currentSeats = ticketType.Seats.ToList();

                var seatsToRemove = currentSeats.Where(s => !selectedSeats.Contains(s.Code)).ToList();
                foreach (var seat in seatsToRemove)
                {
                    _context.Remove(seat);
                }

                // Seats to add
                var existingCodes = currentSeats.Select(s => s.Code).ToHashSet();
                foreach (var seatCode in selectedSeats)
                {
                    if (!existingCodes.Contains(seatCode))
                    {
                        var parts = seatCode.Split('-');
                        if (parts.Length == 2)
                        {
                            var rowLetter = parts[0];
                            if (int.TryParse(parts[1], out int col))
                            {
                                int row = 0;
                                if (rowLetter.Length == 1)
                                {
                                    row = rowLetter[0] - 'A' + 1;
                                }
                                
                                ticketType.Seats.Add(new Seat
                                {
                                    Code = seatCode,
                                    PosX = row,
                                    PosY = col,
                                    Status = SeatStatus.Free
                                });
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task ToggleEventStatusAsync(int id, bool isActive)
        {
            var eventToUpdate = await _context.Events.FindAsync(id);
            if (eventToUpdate != null)
            {
                eventToUpdate.IsActive = isActive;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SubmitEventAsync(int id)
        {
            var eventToUpdate = await _context.Events.FindAsync(id);
            if (eventToUpdate != null)
            {
                eventToUpdate.IsSubmitted = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(int Selling, int Pending, int Finished)> GetEventStatsAsync(int? organizerId = null)
        {
            var query = _context.Events.AsNoTracking().AsQueryable();
            if (organizerId.HasValue)
            {
                query = query.Where(e => e.OrganizerId == organizerId);
            }

            var now = DateTime.Now;
            var stats = await query
                .GroupBy(e => 1)
                .Select(g => new
                {
                    Selling = g.Count(e => e.IsActive && e.IsSubmitted && e.Date > now),
                    Pending = g.Count(e => !e.IsSubmitted),
                    Finished = g.Count(e => e.Date <= now)
                })
                .FirstOrDefaultAsync();

            return stats != null ? (stats.Selling, stats.Pending, stats.Finished) : (0, 0, 0);
        }
    }
}

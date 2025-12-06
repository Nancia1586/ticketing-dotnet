using Microsoft.EntityFrameworkCore;
using Ticketing.BackOffice.Razor.Data;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Pages.Events;
using System.Text.Json;

namespace Ticketing.BackOffice.Razor.Services
{
    public class EventService : IEventService
    {
        private readonly TicketingDbContext _context;

        public EventService(TicketingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            // Inclure les TicketTypes pour affichage dans l'admin si nécessaire
            return await _context.Events
                                 .Include(e => e.TicketTypes)
                                 .Include(e => e.Venue)
                                 .ToListAsync();
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
            _context.Events.Update(eventToUpdate);
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
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task UpdateEventPlanAsync(int eventId, List<PlanModel.TicketTypePlanInputModel> ticketTypePlans)
        {
            var eventToUpdate = await GetEventWithPlanByIdAsync(eventId);
            if (eventToUpdate == null) return;

            // Dimensions are now managed by Venue, so we don't update them here.

            // 2. Sync Ticket Types
            var inputTypeIds = ticketTypePlans.Where(p => p.TicketTypeId != 0).Select(p => p.TicketTypeId).ToList();
            
            // Remove deleted types
            var typesToRemove = eventToUpdate.TicketTypes.Where(tt => !inputTypeIds.Contains(tt.Id)).ToList();
            foreach (var type in typesToRemove)
            {
                _context.Remove(type); 
            }

            foreach (var planInput in ticketTypePlans)
            {
                TicketType ticketType;

                if (planInput.TicketTypeId == 0)
                {
                    // Create new
                    ticketType = new TicketType
                    {
                        EventId = eventId,
                        Name = planInput.Name,
                        Price = planInput.Price,
                        Color = planInput.Color,
                        IsReservedSeating = planInput.IsReservedSeating
                    };
                    eventToUpdate.TicketTypes.Add(ticketType);
                }
                else
                {
                    // Update existing
                    ticketType = eventToUpdate.TicketTypes.FirstOrDefault(tt => tt.Id == planInput.TicketTypeId);
                    if (ticketType != null)
                    {
                        ticketType.Name = planInput.Name;
                        ticketType.Price = planInput.Price;
                        ticketType.Color = planInput.Color;
                        ticketType.IsReservedSeating = planInput.IsReservedSeating;
                    }
                    else
                    {
                        continue; 
                    }
                }

                // 3. Sync Seats
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

                // Seats to remove
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

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
            return await _context.Venues.ToListAsync();
        }
    }
}
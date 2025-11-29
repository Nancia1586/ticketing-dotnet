using Microsoft.EntityFrameworkCore;
using Ticketing.BackOffice.Razor.Data;
using Ticketing.Core.Models;

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
                                 .ToListAsync();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _context.Events.FindAsync(id);
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

        public async Task ToggleEventStatusAsync(int id, bool isActive)
        {
            var eventToUpdate = await _context.Events.FindAsync(id);
            if (eventToUpdate != null)
            {
                eventToUpdate.IsActive = isActive;
                await _context.SaveChangesAsync();
            }
        }
    }
}
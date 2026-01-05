using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IEventService
    {
        Task<IEnumerable<Event>> GetAllEventsAsync(int? organizerId = null);
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event newEvent);
        Task UpdateEventAsync(Event eventToUpdate);
        Task DeleteEventAsync(int id);

        Task<Event?> GetEventWithPlanByIdAsync(int id);
        Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans);
        Task ToggleEventStatusAsync(int id, bool isActive);
        Task SubmitEventAsync(int id);
        
        Task<IEnumerable<Venue>> GetAllVenuesAsync();
    }
}
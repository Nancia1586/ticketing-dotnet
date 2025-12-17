using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event newEvent);
        Task UpdateEventAsync(Event eventToUpdate);
        Task DeleteEventAsync(int id);

        Task ToggleEventStatusAsync(int id, bool isActive);

        Task<Event?> GetEventWithPlanByIdAsync(int id);
        Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans);
        

    }
}

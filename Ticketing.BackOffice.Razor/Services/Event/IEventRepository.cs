using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IEventRepository
    {
        Task<PagedResult<Event>> GetAllEventsAsync(int? organizerId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 10);
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event newEvent);
        Task UpdateEventAsync(Event eventToUpdate);
        Task DeleteEventAsync(int id);

        Task ToggleEventStatusAsync(int id, bool isActive);

        Task<Event?> GetEventWithPlanByIdAsync(int id);
        Task<Event?> GetEventWithDetailsByIdAsync(int id);
        Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans);
        Task SubmitEventAsync(int id);
        
        Task<(int Selling, int Pending, int Finished)> GetEventStatsAsync(int? organizerId = null);
    }
}

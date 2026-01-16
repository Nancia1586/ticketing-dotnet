using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IEventService
    {
        Task<PagedResult<Event>> GetAllEventsAsync(int? organizerId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 10);
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event newEvent);
        Task UpdateEventAsync(Event eventToUpdate);
        Task DeleteEventAsync(int id);

        Task<Event?> GetEventWithPlanByIdAsync(int id);
        Task<Event?> GetEventWithDetailsByIdAsync(int id);
        Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans);
        Task ToggleEventStatusAsync(int id, bool isActive);
        Task SubmitEventAsync(int id);
        
        Task<IEnumerable<Venue>> GetAllVenuesAsync();
        Task<(int Selling, int Pending, int Finished)> GetEventStatsAsync(int? organizerId = null);
    }
}
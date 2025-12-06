using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Pages.Events;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IEventService
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event newEvent);
        Task UpdateEventAsync(Event eventToUpdate);
        Task DeleteEventAsync(int id);

        Task ToggleEventStatusAsync(int id, bool isActive);

        Task<Event?> GetEventWithPlanByIdAsync(int id);
        Task UpdateEventPlanAsync(int eventId, List<PlanModel.TicketTypePlanInputModel> ticketTypePlans);
        
        Task<IEnumerable<Venue>> GetAllVenuesAsync();
    }
}
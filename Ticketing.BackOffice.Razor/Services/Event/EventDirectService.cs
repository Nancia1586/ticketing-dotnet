using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public class EventDirectService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IVenueService _venueService;

        public EventDirectService(IEventRepository eventRepository, IVenueService venueService)
        {
            _eventRepository = eventRepository;
            _venueService = venueService;
        }

        public async Task<PagedResult<Event>> GetAllEventsAsync(int? organizerId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 10)
        {
            return await _eventRepository.GetAllEventsAsync(organizerId, searchTerm, pageNumber, pageSize);
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _eventRepository.GetEventByIdAsync(id);
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            return await _eventRepository.CreateEventAsync(newEvent);
        }

        public async Task UpdateEventAsync(Event eventToUpdate)
        {
            await _eventRepository.UpdateEventAsync(eventToUpdate);
        }

        public async Task DeleteEventAsync(int id)
        {
            await _eventRepository.DeleteEventAsync(id);
        }

        public async Task<Event?> GetEventWithPlanByIdAsync(int id)
        {
            return await _eventRepository.GetEventWithPlanByIdAsync(id);
        }

        public async Task<Event?> GetEventWithDetailsByIdAsync(int id)
        {
            return await _eventRepository.GetEventWithDetailsByIdAsync(id);
        }

        public async Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans)
        {
            await _eventRepository.UpdateEventPlanAsync(eventId, ticketTypePlans);
        }

        public async Task ToggleEventStatusAsync(int id, bool isActive)
        {
            await _eventRepository.ToggleEventStatusAsync(id, isActive);
        }

        public async Task SubmitEventAsync(int id)
        {
            await _eventRepository.SubmitEventAsync(id);
        }

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
            return await _venueService.GetAllVenuesAsync();
        }

        public async Task<(int Selling, int Pending, int Finished)> GetEventStatsAsync(int? organizerId = null)
        {
            return await _eventRepository.GetEventStatsAsync(organizerId);
        }
    }
}


using Ticketing.Core.Models;
using System.Net.Http.Json;

namespace Ticketing.BackOffice.Razor.Services
{
    public class EventApiService : IEventService
    {
        private readonly HttpClient _httpClient;

        public EventApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClient;
            
            var request = httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                // Dynamically detect the port we are running on
                var baseUrl = $"{request.Scheme}://{request.Host}";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
            else if (_httpClient.BaseAddress == null)
            {
                var configUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7281";
                _httpClient.BaseAddress = new Uri(configUrl);
            }
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync(int? organizerId = null)
        {
            var url = "api/events";
            if (organizerId.HasValue)
            {
                url += $"?organizerId={organizerId}";
            }
            return await _httpClient.GetFromJsonAsync<IEnumerable<Event>>(url) ?? new List<Event>();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            try 
            {
                return await _httpClient.GetFromJsonAsync<Event>($"api/events/{id}");
            } 
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) 
            {
                return null;
            }
        }

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            var response = await _httpClient.PostAsJsonAsync("api/events", newEvent);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<Event>();
            if (result == null) throw new InvalidOperationException("Returned event is null.");
            return result;
        }

        public async Task UpdateEventAsync(Event eventToUpdate)
        {
             var response = await _httpClient.PutAsJsonAsync($"api/events/{eventToUpdate.Id}", eventToUpdate);
             response.EnsureSuccessStatusCode();
        }

        public async Task DeleteEventAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/events/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task ToggleEventStatusAsync(int id, bool isActive)
        {
             var response = await _httpClient.PostAsJsonAsync($"api/events/{id}/toggle", isActive);
             response.EnsureSuccessStatusCode();
        }

        public async Task SubmitEventAsync(int id)
        {
            var response = await _httpClient.PostAsync($"api/events/{id}/submit", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<Event?> GetEventWithPlanByIdAsync(int id)
        {
            try 
            {
                return await _httpClient.GetFromJsonAsync<Event>($"api/events/{id}/plan");
            } 
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) 
            {
                return null;
            }
        }

        public async Task UpdateEventPlanAsync(int eventId, List<TicketTypePlanDto> ticketTypePlans)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/events/{eventId}/plan", ticketTypePlans);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
        {
             return await _httpClient.GetFromJsonAsync<IEnumerable<Venue>>("api/venues") ?? new List<Venue>();
        }
    }
}

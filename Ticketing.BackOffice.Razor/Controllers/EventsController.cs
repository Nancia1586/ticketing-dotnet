using Microsoft.AspNetCore.Mvc;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;
using System.Text.Json;

namespace Ticketing.BackOffice.Razor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;

        public EventsController(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents([FromQuery] int? organizerId = null)
        {
            try 
            {
                var events = await _eventRepository.GetAllEventsAsync(organizerId);
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                var json = JsonSerializer.Serialize(events, options);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error fetching and serializing events", 
                    error = ex.Message, 
                    inner = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var evt = await _eventRepository.GetEventByIdAsync(id);
            if (evt == null)
            {
                return NotFound();
            }
            return Ok(evt);
        }

        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event newEvent)
        {
            var createdEvent = await _eventRepository.CreateEventAsync(newEvent);
            return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event eventToUpdate)
        {
            if (id != eventToUpdate.Id)
            {
                return BadRequest();
            }

            await _eventRepository.UpdateEventAsync(eventToUpdate);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            await _eventRepository.DeleteEventAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/toggle")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            await _eventRepository.ToggleEventStatusAsync(id, isActive);
            return NoContent();
        }

        [HttpGet("{id}/details")]
        public async Task<ActionResult<Event>> GetEventWithDetails(int id)
        {
            try 
            {
                var evt = await _eventRepository.GetEventWithDetailsByIdAsync(id);
                if (evt == null) return NotFound();

                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                var json = JsonSerializer.Serialize(evt, options);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error fetching and serializing event details", 
                    error = ex.Message, 
                    inner = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("{id}/plan")]
        public async Task<ActionResult<Event>> GetEventWithPlan(int id)
        {
            try 
            {
                var evt = await _eventRepository.GetEventWithPlanByIdAsync(id);
                if (evt == null) return NotFound();

                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };
                var json = JsonSerializer.Serialize(evt, options);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error fetching and serializing event plan", 
                    error = ex.Message, 
                    inner = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpPut("{id}/plan")]
        public async Task<IActionResult> UpdateEventPlan(int id, [FromBody] List<TicketTypePlanDto> ticketTypePlans)
        {
            await _eventRepository.UpdateEventPlanAsync(id, ticketTypePlans);
            return NoContent();
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitEvent(int id)
        {
            await _eventRepository.SubmitEventAsync(id);
            return NoContent();
        }

    }
}

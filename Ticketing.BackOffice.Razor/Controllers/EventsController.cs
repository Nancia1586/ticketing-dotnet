using Microsoft.AspNetCore.Mvc;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

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
            var events = await _eventRepository.GetAllEventsAsync(organizerId);
            return Ok(events);
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

        [HttpGet("{id}/plan")]
        public async Task<ActionResult<Event>> GetEventWithPlan(int id)
        {
            try 
            {
                var evt = await _eventRepository.GetEventWithPlanByIdAsync(id);
                if (evt == null) return NotFound();
                return Ok(evt);
            }
            catch (Exception ex)
            {
                // Return detailed error for debugging
                return StatusCode(500, new { 
                    message = "Internal Server Error in GetEventWithPlan", 
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

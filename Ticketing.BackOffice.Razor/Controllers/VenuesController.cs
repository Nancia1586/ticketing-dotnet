using Microsoft.AspNetCore.Mvc;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        private readonly IVenueService _venueService;

        public VenuesController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Venue>>> GetVenues()
        {
            return Ok(await _venueService.GetAllVenuesAsync());
        }
    }
}

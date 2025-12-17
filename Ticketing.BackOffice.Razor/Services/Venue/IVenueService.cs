using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IVenueService
    {
        Task<IEnumerable<Venue>> GetAllVenuesAsync();
    }
}

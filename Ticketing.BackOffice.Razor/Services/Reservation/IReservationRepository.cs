using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IReservationRepository
    {
        Task<IEnumerable<Reservation>> GetAllReservationsAsync(int? organizerId = null, string? searchTerm = null);
        Task<Reservation?> GetReservationByIdAsync(int id);
        Task UpdateReservationStatusAsync(int id, ReservationStatus status);
    }
}

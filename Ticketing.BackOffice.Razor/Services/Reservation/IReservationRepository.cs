using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public interface IReservationRepository
    {
        Task<IEnumerable<Reservation>> GetAllReservationsAsync(int? organizerId = null);
        Task<Reservation?> GetReservationByIdAsync(int id);
        Task UpdateReservationStatusAsync(int id, ReservationStatus status);
    }
}

using Ticketing.Core.Models;

namespace Ticketing.BackOffice.Razor.Services
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class DashboardStats
    {
        public int TotalReservations { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<TopEventStats> TopEvents { get; set; } = new();
    }

    public class TopEventStats
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public interface IReservationRepository
    {
        Task<PagedResult<Reservation>> GetAllReservationsAsync(int? organizerId = null, string? searchTerm = null, int pageNumber = 1, int pageSize = 10);
        Task<Reservation?> GetReservationByIdAsync(int id);
        Task UpdateReservationStatusAsync(int id, ReservationStatus status);
        Task<DashboardStats> GetDashboardStatsAsync(int? organizerId = null);
        Task<List<Reservation>> GetAllReservationsForExportAsync(int? organizerId = null, string? searchTerm = null);
    }
}

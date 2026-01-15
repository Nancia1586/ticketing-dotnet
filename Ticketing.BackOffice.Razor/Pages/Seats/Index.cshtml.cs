using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Models;
using Ticketing.Core.Data;
using Ticketing.BackOffice.Razor.Services;

namespace Ticketing.BackOffice.Razor.Pages.Seats
{
    public class IndexModel : PageModel
    {
        private readonly TicketingDbContext _context;
        private readonly IEventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            TicketingDbContext context, 
            IEventService eventService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _eventService = eventService;
            _userManager = userManager;
        }

        public List<SeatViewModel> Seats { get; set; } = new List<SeatViewModel>();
        public List<Event> Events { get; set; } = new List<Event>();
        public int? SelectedEventId { get; set; }
        public SeatStatus? SelectedStatus { get; set; }
        public string? SearchTerm { get; set; }

        public int TotalSeats { get; set; }
        public int FreeSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int TakenSeats { get; set; }
        public int HeldSeats { get; set; }

        public async Task OnGetAsync(int? eventId = null, SeatStatus? status = null, string? searchTerm = null)
        {
            SelectedEventId = eventId;
            SelectedStatus = status;
            SearchTerm = searchTerm;

            int? organizerId = null;
            if (User.IsInRole("Organizer"))
            {
                var user = await _userManager.GetUserAsync(User);
                organizerId = user?.OrganizationId;
            }

            Events = (await _eventService.GetAllEventsAsync(organizerId)).ToList();

            var query = _context.Seats
                .Include(s => s.TicketType)
                    .ThenInclude(tt => tt.Event)
                .Include(s => s.Reservation)
                .AsQueryable();

            if (organizerId.HasValue)
            {
                query = query.Where(s => s.TicketType.Event.OrganizerId == organizerId);
            }

            if (eventId.HasValue)
            {
                query = query.Where(s => s.TicketType.EventId == eventId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(s => 
                    s.Code.Contains(searchTerm) ||
                    (s.Reservation != null && s.Reservation.CustomerName.Contains(searchTerm)) ||
                    (s.Reservation != null && s.Reservation.Email.Contains(searchTerm))
                );
            }

            var seats = await query
                .OrderBy(s => s.TicketType.Event.Name)
                .ThenBy(s => s.Code)
                .ToListAsync();

            Seats = seats.Select(s => new SeatViewModel
            {
                Id = s.Id,
                Code = s.Code,
                Status = s.Status,
                EventName = s.TicketType.Event.Name,
                EventId = s.TicketType.EventId,
                TicketTypeName = s.TicketType.Name,
                Price = s.TicketType.Price,
                ReservationId = s.ReservationId,
                CustomerName = s.Reservation?.CustomerName,
                ReservationStatus = s.Reservation?.Status,
                ReservationReference = s.Reservation?.Reference,
                ReservationDate = s.Reservation?.ReservationDate
            }).ToList();

            TotalSeats = Seats.Count;
            FreeSeats = Seats.Count(s => s.Status == SeatStatus.Free);
            ReservedSeats = Seats.Count(s => s.Status == SeatStatus.Reserved);
            TakenSeats = Seats.Count(s => s.Status == SeatStatus.Taken);
            HeldSeats = Seats.Count(s => s.Status == SeatStatus.Held);
        }

        public class SeatViewModel
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public SeatStatus Status { get; set; }
            public string EventName { get; set; } = string.Empty;
            public int EventId { get; set; }
            public string TicketTypeName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int? ReservationId { get; set; }
            public string? CustomerName { get; set; }
            public ReservationStatus? ReservationStatus { get; set; }
            public string? ReservationReference { get; set; }
            public DateTime? ReservationDate { get; set; }
        }
    }
}




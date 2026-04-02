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

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public async Task OnGetAsync(int? eventId = null, SeatStatus? status = null, string? searchTerm = null, int page = 1)
        {
            SelectedEventId = eventId;
            SelectedStatus = status;
            SearchTerm = searchTerm;
            CurrentPage = page < 1 ? 1 : page;

            int? organizerId = null;
            ApplicationUser? currentUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUser = await _userManager.GetUserAsync(User);
                organizerId = currentUser?.OrganizationId;
            }
            ViewData["CurrentUser"] = currentUser;

            // Load only events without a seating plan (no grid venue)
            var eventsTask = _context.Events
                .Where(e => organizerId == null || e.OrganizerId == organizerId)
                .Where(e => e.IsActive || e.Date >= DateTime.Now.AddMonths(-3))
                .Where(e => e.Venue == null || (e.Venue.TotalRows == 0 && e.Venue.TotalColumns == 0))
                .OrderByDescending(e => e.Date)
                .Take(50)
                .Select(e => new Event { Id = e.Id, Name = e.Name })
                .AsNoTracking()
                .ToListAsync();

            // Build query — only seats from events without a seating plan (no venue grid)
            var query = from seat in _context.Seats
                       join ticketType in _context.TicketTypes on seat.TicketTypeId equals ticketType.Id
                       join evt in _context.Events on ticketType.EventId equals evt.Id
                       join venue in _context.Venues on evt.VenueId equals venue.Id into venueGroup
                       from venue in venueGroup.DefaultIfEmpty()
                       join reservation in _context.Reservations on seat.ReservationId equals reservation.Id into reservationGroup
                       from reservation in reservationGroup.DefaultIfEmpty()
                       where venue == null || (venue.TotalRows == 0 && venue.TotalColumns == 0)
                       select new
                       {
                           Seat = seat,
                           TicketType = ticketType,
                           Event = evt,
                           Reservation = reservation
                       };

            // Apply filters
            if (organizerId.HasValue)
            {
                query = query.Where(x => x.Event.OrganizerId == organizerId);
            }

            if (eventId.HasValue)
            {
                query = query.Where(x => x.Event.Id == eventId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Seat.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(x => 
                    x.Seat.Code.Contains(searchTerm) ||
                    (x.Reservation != null && x.Reservation.CustomerName.Contains(searchTerm)) ||
                    (x.Reservation != null && x.Reservation.Email.Contains(searchTerm)));
            }

            // OPTIMIZED: Calculate statistics and fetch events in parallel
            var statsQuery = query
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Free = g.Count(x => x.Seat.Status == SeatStatus.Free),
                    Reserved = g.Count(x => x.Seat.Status == SeatStatus.Reserved),
                    Taken = g.Count(x => x.Seat.Status == SeatStatus.Taken),
                    Held = g.Count(x => x.Seat.Status == SeatStatus.Held)
                });

            Events = await eventsTask;
            var stats = await statsQuery.FirstOrDefaultAsync();

            // Set statistics
            if (stats != null)
            {
                TotalSeats = stats.Total;
                FreeSeats = stats.Free;
                ReservedSeats = stats.Reserved;
                TakenSeats = stats.Taken;
                HeldSeats = stats.Held;
            }
            else
            {
                TotalSeats = FreeSeats = ReservedSeats = TakenSeats = HeldSeats = 0;
            }

            // Calculate pagination info
            TotalPages = (int)Math.Ceiling(TotalSeats / (double)PageSize);
            
            // Validate page number doesn't exceed total pages
            if (TotalPages > 0 && CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            HasPreviousPage = CurrentPage > 1;
            HasNextPage = CurrentPage < TotalPages;

            // OPTIMIZED: Project to SeatViewModel directly in query (single SQL query)
            var seatsQuery = query
                .Select(x => new SeatViewModel
                {
                    Id = x.Seat.Id,
                    Code = x.Seat.Code,
                    Status = x.Seat.Status,
                    EventName = x.Event.Name,
                    EventId = x.Event.Id,
                    TicketTypeName = x.TicketType.Name,
                    Price = x.TicketType.Price,
                    ReservationId = x.Seat.ReservationId,
                    CustomerName = x.Reservation != null ? x.Reservation.CustomerName : null,
                    ReservationStatus = x.Reservation != null ? (ReservationStatus?)x.Reservation.Status : null,
                    ReservationReference = x.Reservation != null ? x.Reservation.Reference : null,
                    ReservationDate = x.Reservation != null ? (DateTime?)x.Reservation.ReservationDate : null
                })
                .OrderBy(s => s.EventName)
                .ThenBy(s => s.Code)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize);

            Seats = await seatsQuery.AsNoTracking().ToListAsync();
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




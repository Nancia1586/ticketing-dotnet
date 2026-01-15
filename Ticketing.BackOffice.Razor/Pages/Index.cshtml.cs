using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Ticketing.BackOffice.Razor.Services;
using Ticketing.Core.Models;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Ticketing.BackOffice.Razor.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IEventService _eventService;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        ILogger<IndexModel> logger, 
        IEventService eventService, 
        IReservationRepository reservationRepository,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _eventService = eventService;
        _reservationRepository = reservationRepository;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    public int EventsSellingCount { get; set; }
    public int EventsPendingCount { get; set; }
    public int EventsFinishedCount { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalReservations { get; set; }

    public List<CategoryStats> CategoryStatsList { get; set; } = new();
    public List<EventStats> TopEvents { get; set; } = new();

    public class CategoryStats
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class EventStats
    {
        public string Name { get; set; } = "";
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public async Task OnGetAsync()
    {
        int? organizerId = null;
        if (User.IsInRole("Organizer"))
        {
            var user = await _userManager.GetUserAsync(User);
            organizerId = user?.OrganizationId;
        }

        var events = (await _eventService.GetAllEventsAsync(organizerId)).ToList();
        var reservations = (await _reservationRepository.GetAllReservationsAsync(organizerId, null)).ToList();
        var categories = (await _categoryService.GetAllCategoriesAsync()).ToList();
    
        EventsSellingCount = events.Count(e => e.IsActive && e.IsSubmitted && e.Date > DateTime.Now);
        EventsPendingCount = events.Count(e => !e.IsSubmitted);
        EventsFinishedCount = events.Count(e => e.Date <= DateTime.Now);

        var confirmedReservations = reservations.Where(r => r.Status == Ticketing.Core.Models.ReservationStatus.Confirmed).ToList();
        TotalTicketsSold = confirmedReservations.Sum(r => r.SeatCount);
        TotalRevenue = confirmedReservations.Sum(r => r.TotalAmount);
        TotalReservations = reservations.Count;

        CategoryStatsList = categories.Select(c => new CategoryStats
        {
            Name = c.Name,
            Count = events.Count(e => e.CategoryId == c.Id)
        }).Where(s => s.Count > 0).ToList();

        TopEvents = events.Select(e => {
            var eventReservations = confirmedReservations.Where(r => r.EventId == e.Id).ToList();
            return new EventStats
            {
                Name = e.Name,
                TicketsSold = eventReservations.Sum(r => r.SeatCount),
                Revenue = eventReservations.Sum(r => r.TotalAmount)
            };
        }).OrderByDescending(e => e.TicketsSold)
          .Take(5)
          .ToList();
    }
}

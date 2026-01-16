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
        ApplicationUser? currentUser = null;
        if (User.IsInRole("Organizer"))
        {
            currentUser = await _userManager.GetUserAsync(User);
            organizerId = currentUser?.OrganizationId;
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            currentUser = await _userManager.GetUserAsync(User);
        }

        // Store user in ViewData to avoid second DB call in _Layout.cshtml
        ViewData["CurrentUser"] = currentUser;

        // OPTIMIZED: Use SQL aggregation instead of loading all events and all categories
        var eventStats = await _eventService.GetEventStatsAsync(organizerId);
        var categoryCounts = await _categoryService.GetCategoryCountsAsync(organizerId);
        var dashboardStats = await _reservationRepository.GetDashboardStatsAsync(organizerId);
        
        EventsSellingCount = eventStats.Selling;
        EventsPendingCount = eventStats.Pending;
        EventsFinishedCount = eventStats.Finished;

        TotalTicketsSold = dashboardStats.TotalTicketsSold;
        TotalRevenue = dashboardStats.TotalRevenue;
        TotalReservations = dashboardStats.TotalReservations;

        CategoryStatsList = categoryCounts.Select(c => new CategoryStats
        {
            Name = c.Name,
            Count = c.Count
        }).ToList();

        TopEvents = dashboardStats.TopEvents.Select(e => new EventStats
        {
            Name = e.EventName,
            TicketsSold = e.TicketsSold,
            Revenue = e.Revenue
        }).ToList();
    }
}

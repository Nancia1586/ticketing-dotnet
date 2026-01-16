using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Models;
using Ticketing.FrontOffice.Mvc.Services;
using Ticketing.Core.Models;

namespace Ticketing.FrontOffice.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataAccessService _dataAccess;

    public HomeController(ILogger<HomeController> logger, DataAccessService dataAccess)
    {
        _logger = logger;
        _dataAccess = dataAccess;
    }

    public async Task<IActionResult> Index(int? categoryId = null)
    {
        try
        {
            // Get all events for display (filtered by category if selected)
            var allEvents = await _dataAccess.GetActiveEventsAsync(categoryId: categoryId);
            var categories = await _dataAccess.GetAllCategoriesAsync();
            
            // Featured events for carousel (first 5, regardless of category filter)
            var featuredEvents = categoryId.HasValue 
                ? allEvents.Take(5).ToList()
                : (await _dataAccess.GetActiveEventsAsync()).Take(5).ToList();
            
            ViewData["Categories"] = categories;
            ViewData["SelectedCategoryId"] = categoryId;
            ViewData["AllEvents"] = allEvents; // All events for the events section
            
            return View(featuredEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading events");
            ViewData["Categories"] = new List<Category>();
            ViewData["AllEvents"] = new List<Event>();
            return View(new List<Event>());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

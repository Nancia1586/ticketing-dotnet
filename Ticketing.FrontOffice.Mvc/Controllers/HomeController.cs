using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ticketing.FrontOffice.Mvc.Models;
using Ticketing.FrontOffice.Mvc.Services;

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

    public async Task<IActionResult> Index()
    {
        var events = await _dataAccess.GetActiveEventsAsync(null, null);
        var featuredEvents = events.Take(5).ToList(); // Get top 5 for carousel
        return View(featuredEvents);
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

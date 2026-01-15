using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.Core.Models;
using Ticketing.FrontOffice.Mvc.Services;

namespace Ticketing.FrontOffice.Mvc.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly DataAccessService _dataAccess;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TicketingDbContext _context;

        public OrganizerController(
            DataAccessService dataAccess,
            UserManager<ApplicationUser> userManager,
            TicketingDbContext context)
        {
            _dataAccess = dataAccess;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Organizer organizer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    var existingUser = await _userManager.FindByEmailAsync(organizer.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "An account with this email already exists.");
                        return View(organizer);
                    }

                    // Create the Organizer record
                    var organizerId = await _dataAccess.CreateOrganizerAsync(organizer);

                    // Create the ApplicationUser account for sign-in
                    var user = new ApplicationUser
                    {
                        UserName = organizer.Email,
                        Email = organizer.Email,
                        FullName = organizer.Name,
                        OrganizationId = organizerId,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, organizer.Password);
                    if (result.Succeeded)
                    {
                        // Add user to Organizer role
                        await _userManager.AddToRoleAsync(user, "Organizer");
                        return RedirectToAction("Success");
                    }
                    else
                    {
                        // If user creation failed, remove the organizer record
                        var createdOrganizer = await _context.Organizers.FindAsync(organizerId);
                        if (createdOrganizer != null)
                        {
                            _context.Organizers.Remove(createdOrganizer);
                            await _context.SaveChangesAsync();
                        }

                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(organizer);
                    }
                }
                catch (Exception ex)
                {
                    // Log error and show message
                    ModelState.AddModelError("", "Unable to create organizer account. Please try again later.");
                    return View(organizer);
                }
            }
            return View(organizer);
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Models;

namespace Ticketing.Core.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(TicketingDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.Migrate();

            // Seed Roles
            string[] roleNames = { "SysAdmin", "Organizer", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            if (await userManager.FindByEmailAsync("admin@ticketing.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@ticketing.com",
                    Email = "admin@ticketing.com",
                    FullName = "Platform Administrator",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "SysAdmin");
            }

            // Look for any events for further seeding
            if (context.Events.Any() && context.Categories.Any() && context.Venues.Any())
            {
                return;   // DB has been seeded with data
            }

            var organizer = await context.Organizers.FirstOrDefaultAsync(o => o.Email == "contact@passion-events.mg");
            if (organizer == null)
            {
                organizer = new Organizer
                {
                    Name = "Passion Events",
                    OrganizationName = "PASSION EVN3",
                    Email = "contact@passion-events.mg",
                    Password = "Password123!" 
                };
                context.Organizers.Add(organizer);
                await context.SaveChangesAsync();
            }

            // Seed Organizer User linked to this Org
            if (await userManager.FindByEmailAsync(organizer.Email) == null)
            {
                var orgUser = new ApplicationUser
                {
                    UserName = organizer.Email,
                    Email = organizer.Email,
                    FullName = organizer.Name,
                    OrganizationId = organizer.Id,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(orgUser, "Organizer123!");
                await userManager.AddToRoleAsync(orgUser, "Organizer");
            }

            // Create default categories if they don't exist
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Cultures", Description = "Événements culturels", IsActive = true },
                    new Category { Name = "Spectacles et concerts", Description = "Spectacles et concerts", IsActive = true },
                    new Category { Name = "Foires et séminaires", Description = "Foires et séminaires", IsActive = true },
                    new Category { Name = "Autre", Description = "Autres types d'événements", IsActive = true }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            var categoriesList = await context.Categories.ToListAsync();

            if (!context.Venues.Any())
            {
                var venue = new Venue
                {
                    Name = "La City Ivandry",
                    Address = "Ivandry, Antananarivo, Madagascar",
                    TotalRows = 10,
                    TotalColumns = 15, 
                    LayoutJson = "{}" 
                };
                context.Venues.Add(venue);
                await context.SaveChangesAsync();
            }

            var venueInDb = await context.Venues.FirstAsync();

            if (!context.Events.Any())
            {
                var evt = new Event
                {
                    Name = "MISENGY TROPICAL NIGHT",
                    Description = "Une nuit inoubliable avec des rythmes tropicaux et une ambiance de folie.",
                    Date = DateTime.Now.AddDays(14).AddHours(20).AddMinutes(30),
                    VenueId = venueInDb.Id,
                    OrganizerId = organizer.Id,
                    CategoryId = categoriesList[1].Id,
                    IsActive = true,
                    PosterUrl = "https://placehold.co/600x400/1a1a1a/FFF?text=MISENGY+TROPICAL+NIGHT"
                };
                context.Events.Add(evt);
                await context.SaveChangesAsync();

                var ticketTypes = new List<TicketType>
                {
                    new TicketType { Name = "Bronze", Price = 40000, TotalCapacity = 50, Color = "#CD7F32", EventId = evt.Id },
                    new TicketType { Name = "Silver", Price = 50000, TotalCapacity = 50, Color = "#C0C0C0", EventId = evt.Id },
                    new TicketType { Name = "Gold", Price = 60000, TotalCapacity = 50, Color = "#FFD700", EventId = evt.Id }
                };
                context.TicketTypes.AddRange(ticketTypes);
                await context.SaveChangesAsync();
                
                var seats = new List<Seat>();
                for (int r = 1; r <= 3; r++)
                    for (int c = 1; c <= 14; c++)
                        seats.Add(new Seat { PosX = r, PosY = c, Status = SeatStatus.Free, TicketTypeId = ticketTypes[2].Id });

                for (int r = 4; r <= 6; r++)
                    for (int c = 1; c <= 14; c++)
                        seats.Add(new Seat { PosX = r, PosY = c, Status = SeatStatus.Free, TicketTypeId = ticketTypes[1].Id });

                for (int r = 7; r <= 10; r++)
                    for (int c = 1; c <= 14; c++)
                        seats.Add(new Seat { PosX = r, PosY = c, Status = SeatStatus.Free, TicketTypeId = ticketTypes[0].Id });

                context.Seats.AddRange(seats);
                await context.SaveChangesAsync();
            }
        }
    }
}

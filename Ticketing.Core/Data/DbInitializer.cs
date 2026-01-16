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

            // Seed Organizer
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

            // Seed Categories - Create if they don't exist
            var categoryNames = new[] { "Cultures", "Spectacles et concerts", "Foires et séminaires", "Autre" };
            var existingCategoryNames = await context.Categories.Select(c => c.Name).ToListAsync();
            
            var newCategories = categoryNames
                .Where(name => !existingCategoryNames.Contains(name))
                .Select(name => new Category 
                { 
                    Name = name, 
                    Description = GetCategoryDescription(name), 
                    IsActive = true 
                })
                .ToList();

            if (newCategories.Any())
            {
                context.Categories.AddRange(newCategories);
                await context.SaveChangesAsync();
            }

            var categoriesList = await context.Categories.ToListAsync();
            var culturesCategory = categoriesList.FirstOrDefault(c => c.Name == "Cultures");
            var spectaclesCategory = categoriesList.FirstOrDefault(c => c.Name == "Spectacles et concerts");
            var foiresCategory = categoriesList.FirstOrDefault(c => c.Name == "Foires et séminaires");
            var autreCategory = categoriesList.FirstOrDefault(c => c.Name == "Autre");

            // Seed Venues - Create if they don't exist
            var venuesToCreate = new List<Venue>
            {
                // Antsahamanitra - Grande salle avec scène centrale
                new Venue 
                { 
                    Name = "Antsahamanitra", 
                    Address = "Antsahamanitra, Antananarivo, Madagascar", 
                    TotalRows = 25, 
                    TotalColumns = 50,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène centrale (lignes 4-9, colonnes 1-3)
                        (4, 1, "void", "Scene"), (4, 2, "void", "Scene"), (4, 3, "void", "Scene"),
                        (5, 1, "void", "Scene"), (5, 2, "void", "Scene"), (5, 3, "void", "Scene"),
                        (6, 1, "void", "Scene"), (6, 2, "void", "Scene"), (6, 3, "void", "Scene"),
                        (7, 1, "void", "Scene"), (7, 2, "void", "Scene"), (7, 3, "void", "Scene"),
                        (8, 1, "void", "Scene"), (8, 2, "void", "Scene"), (8, 3, "void", "Scene"),
                        (9, 1, "void", "Scene"), (9, 2, "void", "Scene"), (9, 3, "void", "Scene"),
                        // Allées latérales (colonnes 24-26)
                        (1, 24, "void", "Allée"), (1, 25, "void", "Allée"), (1, 26, "void", "Allée"),
                        (2, 24, "void", "Allée"), (2, 25, "void", "Allée"), (2, 26, "void", "Allée"),
                        (3, 24, "void", "Allée"), (3, 25, "void", "Allée"), (3, 26, "void", "Allée"),
                        (4, 24, "void", "Allée"), (4, 25, "void", "Allée"), (4, 26, "void", "Allée"),
                        (5, 24, "void", "Allée"), (5, 25, "void", "Allée"), (5, 26, "void", "Allée"),
                        (6, 24, "void", "Allée"), (6, 25, "void", "Allée"), (6, 26, "void", "Allée"),
                        (7, 24, "void", "Allée"), (7, 25, "void", "Allée"), (7, 26, "void", "Allée"),
                        (8, 24, "void", "Allée"), (8, 25, "void", "Allée"), (8, 26, "void", "Allée"),
                        (9, 24, "void", "Allée"), (9, 25, "void", "Allée"), (9, 26, "void", "Allée"),
                        (10, 24, "void", "Allée"), (10, 25, "void", "Allée"), (10, 26, "void", "Allée"),
                        (11, 24, "void", "Allée"), (11, 25, "void", "Allée"), (11, 26, "void", "Allée"),
                        (12, 24, "void", "Allée"), (12, 25, "void", "Allée"), (12, 26, "void", "Allée"),
                        (13, 24, "void", "Allée"), (13, 25, "void", "Allée"), (13, 26, "void", "Allée"),
                        (14, 24, "void", "Allée"), (14, 25, "void", "Allée"), (14, 26, "void", "Allée"),
                        (15, 24, "void", "Allée"), (15, 25, "void", "Allée"), (15, 26, "void", "Allée"),
                        (16, 24, "void", "Allée"), (16, 25, "void", "Allée"), (16, 26, "void", "Allée"),
                        (17, 24, "void", "Allée"), (17, 25, "void", "Allée"), (17, 26, "void", "Allée"),
                        (18, 24, "void", "Allée"), (18, 25, "void", "Allée"), (18, 26, "void", "Allée"),
                        (19, 24, "void", "Allée"), (19, 25, "void", "Allée"), (19, 26, "void", "Allée"),
                        (20, 24, "void", "Allée"), (20, 25, "void", "Allée"), (20, 26, "void", "Allée"),
                        (21, 24, "void", "Allée"), (21, 25, "void", "Allée"), (21, 26, "void", "Allée"),
                        (22, 24, "void", "Allée"), (22, 25, "void", "Allée"), (22, 26, "void", "Allée"),
                        (23, 24, "void", "Allée"), (23, 25, "void", "Allée"), (23, 26, "void", "Allée"),
                        (24, 24, "void", "Allée"), (24, 25, "void", "Allée"), (24, 26, "void", "Allée"),
                        (25, 24, "void", "Allée"), (25, 25, "void", "Allée"), (25, 26, "void", "Allée")
                    })
                },
                // CCI - Salle de conférence avec scène avant
                new Venue 
                { 
                    Name = "CCI", 
                    Address = "CCI Ivato, Antananarivo, Madagascar", 
                    TotalRows = 20, 
                    TotalColumns = 40,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène avant (lignes 1-3, colonnes 1-5)
                        (1, 1, "void", "Scene"), (1, 2, "void", "Scene"), (1, 3, "void", "Scene"), (1, 4, "void", "Scene"), (1, 5, "void", "Scene"),
                        (2, 1, "void", "Scene"), (2, 2, "void", "Scene"), (2, 3, "void", "Scene"), (2, 4, "void", "Scene"), (2, 5, "void", "Scene"),
                        (3, 1, "void", "Scene"), (3, 2, "void", "Scene"), (3, 3, "void", "Scene"), (3, 4, "void", "Scene"), (3, 5, "void", "Scene"),
                        // Allée centrale (colonne 20)
                        (4, 20, "void", "Allée"), (5, 20, "void", "Allée"), (6, 20, "void", "Allée"), (7, 20, "void", "Allée"),
                        (8, 20, "void", "Allée"), (9, 20, "void", "Allée"), (10, 20, "void", "Allée"), (11, 20, "void", "Allée"),
                        (12, 20, "void", "Allée"), (13, 20, "void", "Allée"), (14, 20, "void", "Allée"), (15, 20, "void", "Allée"),
                        (16, 20, "void", "Allée"), (17, 20, "void", "Allée"), (18, 20, "void", "Allée"), (19, 20, "void", "Allée"),
                        (20, 20, "void", "Allée")
                    })
                },
                // ESSCA - Salle académique
                new Venue 
                { 
                    Name = "ESSCA", 
                    Address = "ESSCA Antanimena, Antananarivo, Madagascar", 
                    TotalRows = 15, 
                    TotalColumns = 25,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène (lignes 1-2, colonnes 1-4)
                        (1, 1, "void", "Scene"), (1, 2, "void", "Scene"), (1, 3, "void", "Scene"), (1, 4, "void", "Scene"),
                        (2, 1, "void", "Scene"), (2, 2, "void", "Scene"), (2, 3, "void", "Scene"), (2, 4, "void", "Scene"),
                        // Allée centrale (colonne 13)
                        (3, 13, "void", "Allée"), (4, 13, "void", "Allée"), (5, 13, "void", "Allée"), (6, 13, "void", "Allée"),
                        (7, 13, "void", "Allée"), (8, 13, "void", "Allée"), (9, 13, "void", "Allée"), (10, 13, "void", "Allée"),
                        (11, 13, "void", "Allée"), (12, 13, "void", "Allée"), (13, 13, "void", "Allée"), (14, 13, "void", "Allée"),
                        (15, 13, "void", "Allée")
                    })
                },
                // BAREA - Stade avec terrain central
                new Venue 
                { 
                    Name = "BAREA", 
                    Address = "Stade BAREA Mahamasina, Antananarivo, Madagascar", 
                    TotalRows = 40, 
                    TotalColumns = 60,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Terrain central (lignes 15-25, colonnes 20-40)
                        (15, 20, "void", "Terrain"), (15, 21, "void", "Terrain"), (15, 22, "void", "Terrain"), (15, 23, "void", "Terrain"), (15, 24, "void", "Terrain"),
                        (15, 25, "void", "Terrain"), (15, 26, "void", "Terrain"), (15, 27, "void", "Terrain"), (15, 28, "void", "Terrain"), (15, 29, "void", "Terrain"),
                        (15, 30, "void", "Terrain"), (15, 31, "void", "Terrain"), (15, 32, "void", "Terrain"), (15, 33, "void", "Terrain"), (15, 34, "void", "Terrain"),
                        (15, 35, "void", "Terrain"), (15, 36, "void", "Terrain"), (15, 37, "void", "Terrain"), (15, 38, "void", "Terrain"), (15, 39, "void", "Terrain"), (15, 40, "void", "Terrain"),
                        (16, 20, "void", "Terrain"), (16, 21, "void", "Terrain"), (16, 22, "void", "Terrain"), (16, 23, "void", "Terrain"), (16, 24, "void", "Terrain"),
                        (16, 25, "void", "Terrain"), (16, 26, "void", "Terrain"), (16, 27, "void", "Terrain"), (16, 28, "void", "Terrain"), (16, 29, "void", "Terrain"),
                        (16, 30, "void", "Terrain"), (16, 31, "void", "Terrain"), (16, 32, "void", "Terrain"), (16, 33, "void", "Terrain"), (16, 34, "void", "Terrain"),
                        (16, 35, "void", "Terrain"), (16, 36, "void", "Terrain"), (16, 37, "void", "Terrain"), (16, 38, "void", "Terrain"), (16, 39, "void", "Terrain"), (16, 40, "void", "Terrain"),
                        (17, 20, "void", "Terrain"), (17, 21, "void", "Terrain"), (17, 22, "void", "Terrain"), (17, 23, "void", "Terrain"), (17, 24, "void", "Terrain"),
                        (17, 25, "void", "Terrain"), (17, 26, "void", "Terrain"), (17, 27, "void", "Terrain"), (17, 28, "void", "Terrain"), (17, 29, "void", "Terrain"),
                        (17, 30, "void", "Terrain"), (17, 31, "void", "Terrain"), (17, 32, "void", "Terrain"), (17, 33, "void", "Terrain"), (17, 34, "void", "Terrain"),
                        (17, 35, "void", "Terrain"), (17, 36, "void", "Terrain"), (17, 37, "void", "Terrain"), (17, 38, "void", "Terrain"), (17, 39, "void", "Terrain"), (17, 40, "void", "Terrain"),
                        (18, 20, "void", "Terrain"), (18, 21, "void", "Terrain"), (18, 22, "void", "Terrain"), (18, 23, "void", "Terrain"), (18, 24, "void", "Terrain"),
                        (18, 25, "void", "Terrain"), (18, 26, "void", "Terrain"), (18, 27, "void", "Terrain"), (18, 28, "void", "Terrain"), (18, 29, "void", "Terrain"),
                        (18, 30, "void", "Terrain"), (18, 31, "void", "Terrain"), (18, 32, "void", "Terrain"), (18, 33, "void", "Terrain"), (18, 34, "void", "Terrain"),
                        (18, 35, "void", "Terrain"), (18, 36, "void", "Terrain"), (18, 37, "void", "Terrain"), (18, 38, "void", "Terrain"), (18, 39, "void", "Terrain"), (18, 40, "void", "Terrain"),
                        (19, 20, "void", "Terrain"), (19, 21, "void", "Terrain"), (19, 22, "void", "Terrain"), (19, 23, "void", "Terrain"), (19, 24, "void", "Terrain"),
                        (19, 25, "void", "Terrain"), (19, 26, "void", "Terrain"), (19, 27, "void", "Terrain"), (19, 28, "void", "Terrain"), (19, 29, "void", "Terrain"),
                        (19, 30, "void", "Terrain"), (19, 31, "void", "Terrain"), (19, 32, "void", "Terrain"), (19, 33, "void", "Terrain"), (19, 34, "void", "Terrain"),
                        (19, 35, "void", "Terrain"), (19, 36, "void", "Terrain"), (19, 37, "void", "Terrain"), (19, 38, "void", "Terrain"), (19, 39, "void", "Terrain"), (19, 40, "void", "Terrain"),
                        (20, 20, "void", "Terrain"), (20, 21, "void", "Terrain"), (20, 22, "void", "Terrain"), (20, 23, "void", "Terrain"), (20, 24, "void", "Terrain"),
                        (20, 25, "void", "Terrain"), (20, 26, "void", "Terrain"), (20, 27, "void", "Terrain"), (20, 28, "void", "Terrain"), (20, 29, "void", "Terrain"),
                        (20, 30, "void", "Terrain"), (20, 31, "void", "Terrain"), (20, 32, "void", "Terrain"), (20, 33, "void", "Terrain"), (20, 34, "void", "Terrain"),
                        (20, 35, "void", "Terrain"), (20, 36, "void", "Terrain"), (20, 37, "void", "Terrain"), (20, 38, "void", "Terrain"), (20, 39, "void", "Terrain"), (20, 40, "void", "Terrain"),
                        (21, 20, "void", "Terrain"), (21, 21, "void", "Terrain"), (21, 22, "void", "Terrain"), (21, 23, "void", "Terrain"), (21, 24, "void", "Terrain"),
                        (21, 25, "void", "Terrain"), (21, 26, "void", "Terrain"), (21, 27, "void", "Terrain"), (21, 28, "void", "Terrain"), (21, 29, "void", "Terrain"),
                        (21, 30, "void", "Terrain"), (21, 31, "void", "Terrain"), (21, 32, "void", "Terrain"), (21, 33, "void", "Terrain"), (21, 34, "void", "Terrain"),
                        (21, 35, "void", "Terrain"), (21, 36, "void", "Terrain"), (21, 37, "void", "Terrain"), (21, 38, "void", "Terrain"), (21, 39, "void", "Terrain"), (21, 40, "void", "Terrain"),
                        (22, 20, "void", "Terrain"), (22, 21, "void", "Terrain"), (22, 22, "void", "Terrain"), (22, 23, "void", "Terrain"), (22, 24, "void", "Terrain"),
                        (22, 25, "void", "Terrain"), (22, 26, "void", "Terrain"), (22, 27, "void", "Terrain"), (22, 28, "void", "Terrain"), (22, 29, "void", "Terrain"),
                        (22, 30, "void", "Terrain"), (22, 31, "void", "Terrain"), (22, 32, "void", "Terrain"), (22, 33, "void", "Terrain"), (22, 34, "void", "Terrain"),
                        (22, 35, "void", "Terrain"), (22, 36, "void", "Terrain"), (22, 37, "void", "Terrain"), (22, 38, "void", "Terrain"), (22, 39, "void", "Terrain"), (22, 40, "void", "Terrain"),
                        (23, 20, "void", "Terrain"), (23, 21, "void", "Terrain"), (23, 22, "void", "Terrain"), (23, 23, "void", "Terrain"), (23, 24, "void", "Terrain"),
                        (23, 25, "void", "Terrain"), (23, 26, "void", "Terrain"), (23, 27, "void", "Terrain"), (23, 28, "void", "Terrain"), (23, 29, "void", "Terrain"),
                        (23, 30, "void", "Terrain"), (23, 31, "void", "Terrain"), (23, 32, "void", "Terrain"), (23, 33, "void", "Terrain"), (23, 34, "void", "Terrain"),
                        (23, 35, "void", "Terrain"), (23, 36, "void", "Terrain"), (23, 37, "void", "Terrain"), (23, 38, "void", "Terrain"), (23, 39, "void", "Terrain"), (23, 40, "void", "Terrain"),
                        (24, 20, "void", "Terrain"), (24, 21, "void", "Terrain"), (24, 22, "void", "Terrain"), (24, 23, "void", "Terrain"), (24, 24, "void", "Terrain"),
                        (24, 25, "void", "Terrain"), (24, 26, "void", "Terrain"), (24, 27, "void", "Terrain"), (24, 28, "void", "Terrain"), (24, 29, "void", "Terrain"),
                        (24, 30, "void", "Terrain"), (24, 31, "void", "Terrain"), (24, 32, "void", "Terrain"), (24, 33, "void", "Terrain"), (24, 34, "void", "Terrain"),
                        (24, 35, "void", "Terrain"), (24, 36, "void", "Terrain"), (24, 37, "void", "Terrain"), (24, 38, "void", "Terrain"), (24, 39, "void", "Terrain"), (24, 40, "void", "Terrain"),
                        (25, 20, "void", "Terrain"), (25, 21, "void", "Terrain"), (25, 22, "void", "Terrain"), (25, 23, "void", "Terrain"), (25, 24, "void", "Terrain"),
                        (25, 25, "void", "Terrain"), (25, 26, "void", "Terrain"), (25, 27, "void", "Terrain"), (25, 28, "void", "Terrain"), (25, 29, "void", "Terrain"),
                        (25, 30, "void", "Terrain"), (25, 31, "void", "Terrain"), (25, 32, "void", "Terrain"), (25, 33, "void", "Terrain"), (25, 34, "void", "Terrain"),
                        (25, 35, "void", "Terrain"), (25, 36, "void", "Terrain"), (25, 37, "void", "Terrain"), (25, 38, "void", "Terrain"), (25, 39, "void", "Terrain"), (25, 40, "void", "Terrain")
                    })
                },
                // KIANJA MAINTSO - Espace ouvert avec scène
                new Venue 
                { 
                    Name = "KIANJA MAINTSO", 
                    Address = "Kianja Maintso Anosizato, Antananarivo, Madagascar", 
                    TotalRows = 30, 
                    TotalColumns = 50,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène centrale (lignes 5-8, colonnes 20-30)
                        (5, 20, "void", "Scene"), (5, 21, "void", "Scene"), (5, 22, "void", "Scene"), (5, 23, "void", "Scene"), (5, 24, "void", "Scene"),
                        (5, 25, "void", "Scene"), (5, 26, "void", "Scene"), (5, 27, "void", "Scene"), (5, 28, "void", "Scene"), (5, 29, "void", "Scene"), (5, 30, "void", "Scene"),
                        (6, 20, "void", "Scene"), (6, 21, "void", "Scene"), (6, 22, "void", "Scene"), (6, 23, "void", "Scene"), (6, 24, "void", "Scene"),
                        (6, 25, "void", "Scene"), (6, 26, "void", "Scene"), (6, 27, "void", "Scene"), (6, 28, "void", "Scene"), (6, 29, "void", "Scene"), (6, 30, "void", "Scene"),
                        (7, 20, "void", "Scene"), (7, 21, "void", "Scene"), (7, 22, "void", "Scene"), (7, 23, "void", "Scene"), (7, 24, "void", "Scene"),
                        (7, 25, "void", "Scene"), (7, 26, "void", "Scene"), (7, 27, "void", "Scene"), (7, 28, "void", "Scene"), (7, 29, "void", "Scene"), (7, 30, "void", "Scene"),
                        (8, 20, "void", "Scene"), (8, 21, "void", "Scene"), (8, 22, "void", "Scene"), (8, 23, "void", "Scene"), (8, 24, "void", "Scene"),
                        (8, 25, "void", "Scene"), (8, 26, "void", "Scene"), (8, 27, "void", "Scene"), (8, 28, "void", "Scene"), (8, 29, "void", "Scene"), (8, 30, "void", "Scene"),
                        // Allées (colonnes 15 et 35)
                        (1, 15, "void", "Allée"), (2, 15, "void", "Allée"), (3, 15, "void", "Allée"), (4, 15, "void", "Allée"), (5, 15, "void", "Allée"),
                        (6, 15, "void", "Allée"), (7, 15, "void", "Allée"), (8, 15, "void", "Allée"), (9, 15, "void", "Allée"), (10, 15, "void", "Allée"),
                        (11, 15, "void", "Allée"), (12, 15, "void", "Allée"), (13, 15, "void", "Allée"), (14, 15, "void", "Allée"), (15, 15, "void", "Allée"),
                        (16, 15, "void", "Allée"), (17, 15, "void", "Allée"), (18, 15, "void", "Allée"), (19, 15, "void", "Allée"), (20, 15, "void", "Allée"),
                        (21, 15, "void", "Allée"), (22, 15, "void", "Allée"), (23, 15, "void", "Allée"), (24, 15, "void", "Allée"), (25, 15, "void", "Allée"),
                        (26, 15, "void", "Allée"), (27, 15, "void", "Allée"), (28, 15, "void", "Allée"), (29, 15, "void", "Allée"), (30, 15, "void", "Allée"),
                        (1, 35, "void", "Allée"), (2, 35, "void", "Allée"), (3, 35, "void", "Allée"), (4, 35, "void", "Allée"), (5, 35, "void", "Allée"),
                        (6, 35, "void", "Allée"), (7, 35, "void", "Allée"), (8, 35, "void", "Allée"), (9, 35, "void", "Allée"), (10, 35, "void", "Allée"),
                        (11, 35, "void", "Allée"), (12, 35, "void", "Allée"), (13, 35, "void", "Allée"), (14, 35, "void", "Allée"), (15, 35, "void", "Allée"),
                        (16, 35, "void", "Allée"), (17, 35, "void", "Allée"), (18, 35, "void", "Allée"), (19, 35, "void", "Allée"), (20, 35, "void", "Allée"),
                        (21, 35, "void", "Allée"), (22, 35, "void", "Allée"), (23, 35, "void", "Allée"), (24, 35, "void", "Allée"), (25, 35, "void", "Allée"),
                        (26, 35, "void", "Allée"), (27, 35, "void", "Allée"), (28, 35, "void", "Allée"), (29, 35, "void", "Allée"), (30, 35, "void", "Allée")
                    })
                },
                // La City Ivandry - Salle de concert moyenne
                new Venue 
                { 
                    Name = "La City Ivandry", 
                    Address = "Ivandry, Antananarivo, Madagascar", 
                    TotalRows = 10, 
                    TotalColumns = 15,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène avant (lignes 1-2, colonnes 1-3)
                        (1, 1, "void", "Scene"), (1, 2, "void", "Scene"), (1, 3, "void", "Scene"),
                        (2, 1, "void", "Scene"), (2, 2, "void", "Scene"), (2, 3, "void", "Scene"),
                        // Allée centrale (colonne 8)
                        (3, 8, "void", "Allée"), (4, 8, "void", "Allée"), (5, 8, "void", "Allée"),
                        (6, 8, "void", "Allée"), (7, 8, "void", "Allée"), (8, 8, "void", "Allée"),
                        (9, 8, "void", "Allée"), (10, 8, "void", "Allée")
                    })
                },
                // Palais des Sports - Grande salle polyvalente
                new Venue 
                { 
                    Name = "Palais des Sports", 
                    Address = "Ankorondrano, Antananarivo, Madagascar", 
                    TotalRows = 20, 
                    TotalColumns = 25,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène centrale (lignes 1-3, colonnes 10-15)
                        (1, 10, "void", "Scene"), (1, 11, "void", "Scene"), (1, 12, "void", "Scene"), (1, 13, "void", "Scene"), (1, 14, "void", "Scene"), (1, 15, "void", "Scene"),
                        (2, 10, "void", "Scene"), (2, 11, "void", "Scene"), (2, 12, "void", "Scene"), (2, 13, "void", "Scene"), (2, 14, "void", "Scene"), (2, 15, "void", "Scene"),
                        (3, 10, "void", "Scene"), (3, 11, "void", "Scene"), (3, 12, "void", "Scene"), (3, 13, "void", "Scene"), (3, 14, "void", "Scene"), (3, 15, "void", "Scene"),
                        // Allées latérales (colonnes 8 et 18)
                        (4, 8, "void", "Allée"), (5, 8, "void", "Allée"), (6, 8, "void", "Allée"), (7, 8, "void", "Allée"),
                        (8, 8, "void", "Allée"), (9, 8, "void", "Allée"), (10, 8, "void", "Allée"), (11, 8, "void", "Allée"),
                        (12, 8, "void", "Allée"), (13, 8, "void", "Allée"), (14, 8, "void", "Allée"), (15, 8, "void", "Allée"),
                        (16, 8, "void", "Allée"), (17, 8, "void", "Allée"), (18, 8, "void", "Allée"), (19, 8, "void", "Allée"), (20, 8, "void", "Allée"),
                        (4, 18, "void", "Allée"), (5, 18, "void", "Allée"), (6, 18, "void", "Allée"), (7, 18, "void", "Allée"),
                        (8, 18, "void", "Allée"), (9, 18, "void", "Allée"), (10, 18, "void", "Allée"), (11, 18, "void", "Allée"),
                        (12, 18, "void", "Allée"), (13, 18, "void", "Allée"), (14, 18, "void", "Allée"), (15, 18, "void", "Allée"),
                        (16, 18, "void", "Allée"), (17, 18, "void", "Allée"), (18, 18, "void", "Allée"), (19, 18, "void", "Allée"), (20, 18, "void", "Allée")
                    })
                },
                // Théâtre Municipal - Salle de spectacle classique
                new Venue 
                { 
                    Name = "Théâtre Municipal", 
                    Address = "Analakely, Antananarivo, Madagascar", 
                    TotalRows = 15, 
                    TotalColumns = 20,
                    LayoutJson = CreateSceneLayoutJson(new List<(int r, int c, string type, string label)>
                    {
                        // Scène avant (lignes 1-2, colonnes 1-5)
                        (1, 1, "void", "Scene"), (1, 2, "void", "Scene"), (1, 3, "void", "Scene"), (1, 4, "void", "Scene"), (1, 5, "void", "Scene"),
                        (2, 1, "void", "Scene"), (2, 2, "void", "Scene"), (2, 3, "void", "Scene"), (2, 4, "void", "Scene"), (2, 5, "void", "Scene"),
                        // Allée centrale (colonne 10)
                        (3, 10, "void", "Allée"), (4, 10, "void", "Allée"), (5, 10, "void", "Allée"), (6, 10, "void", "Allée"),
                        (7, 10, "void", "Allée"), (8, 10, "void", "Allée"), (9, 10, "void", "Allée"), (10, 10, "void", "Allée"),
                        (11, 10, "void", "Allée"), (12, 10, "void", "Allée"), (13, 10, "void", "Allée"), (14, 10, "void", "Allée"),
                        (15, 10, "void", "Allée")
                    })
                }
            };

            var existingVenueNames = await context.Venues.Select(v => v.Name).ToListAsync();
            var newVenues = venuesToCreate
                .Where(v => !existingVenueNames.Contains(v.Name))
                .ToList();

            if (newVenues.Any())
            {
                context.Venues.AddRange(newVenues);
                await context.SaveChangesAsync();
            }

            var venuesList = await context.Venues.ToListAsync();
            var antsahamanitraVenue = venuesList.FirstOrDefault(v => v.Name == "Antsahamanitra");
            var cciVenue = venuesList.FirstOrDefault(v => v.Name == "CCI");
            var esscaVenue = venuesList.FirstOrDefault(v => v.Name == "ESSCA");
            var bareaVenue = venuesList.FirstOrDefault(v => v.Name == "BAREA");
            var kianjaMaitsoVenue = venuesList.FirstOrDefault(v => v.Name == "KIANJA MAINTSO");

            // Seed Events - Create test events if they don't exist
            var testEventCodes = new[] { "MISENGY", "FESTIVAL", "SEMINAIRE", "ROCKNIGHT", "EXPOART" };
            var existingEventCodes = await context.Events.Select(e => e.Code).Where(c => !string.IsNullOrEmpty(c)).ToListAsync();
            var shouldCreateEvents = !existingEventCodes.Any() || !testEventCodes.Any(code => existingEventCodes.Contains(code));
            
            if (shouldCreateEvents)
            {
                var events = new List<Event>();

                // Event 1: Concert at Antsahamanitra
                if (antsahamanitraVenue != null && spectaclesCategory != null)
                {
                    var evt1 = new Event
                    {
                        Name = "MISENGY TROPICAL NIGHT",
                        Description = "Une nuit inoubliable avec des rythmes tropicaux et une ambiance de folie à Antsahamanitra.",
                        Code = "MISENGY",
                        Date = DateTime.Now.AddDays(14).AddHours(20).AddMinutes(30),
                        VenueId = antsahamanitraVenue.Id,
                        OrganizerId = organizer.Id,
                        CategoryId = spectaclesCategory.Id,
                        IsActive = true,
                        IsSubmitted = true,
                        PosterUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&h=600&fit=crop&q=80"
                    };
                    events.Add(evt1);
                }

                // Event 2: Festival at BAREA
                if (bareaVenue != null && culturesCategory != null)
                {
                    var evt2 = new Event
                    {
                        Name = "Grande Braderie de Madagascar",
                        Description = "Le plus grand rassemblement commercial et culturel au Stade Barea.",
                        Code = "FESTIVAL",
                        Date = DateTime.Now.AddDays(21).AddHours(18).AddMinutes(0),
                        VenueId = bareaVenue.Id,
                        OrganizerId = organizer.Id,
                        CategoryId = culturesCategory.Id,
                        IsActive = true,
                        IsSubmitted = true,
                        PosterUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=800&h=600&fit=crop&q=80"
                    };
                    events.Add(evt2);
                }

                // Event 3: Séminaire at CCI Ivato
                if (cciVenue != null && foiresCategory != null)
                {
                    var evt3 = new Event
                    {
                        Name = "Salon de l'Auto 2024",
                        Description = "Exposition automobile internationale au CCI Ivato.",
                        Code = "SEMINAIRE",
                        Date = DateTime.Now.AddDays(30).AddHours(9).AddMinutes(0),
                        VenueId = cciVenue.Id,
                        OrganizerId = organizer.Id,
                        CategoryId = foiresCategory.Id,
                        IsActive = true,
                        IsSubmitted = true,
                        PosterUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800&h=600&fit=crop&q=80"
                    };
                    events.Add(evt3);
                }

                // Event 4: Concert Rock at Kianja Maitso
                if (kianjaMaitsoVenue != null && spectaclesCategory != null)
                {
                    var evt4 = new Event
                    {
                        Name = "Rock Night Madagascar",
                        Description = "Une soirée rock électrisante à Kianja Maitso.",
                        Code = "ROCKNIGHT",
                        Date = DateTime.Now.AddDays(45).AddHours(21).AddMinutes(0),
                        VenueId = kianjaMaitsoVenue.Id,
                        OrganizerId = organizer.Id,
                        CategoryId = spectaclesCategory.Id,
                        IsActive = true,
                        IsSubmitted = true,
                        PosterUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=800&h=600&fit=crop&q=80"
                    };
                    events.Add(evt4);
                }

                // Event 5: Gala at ESSCA
                if (esscaVenue != null && culturesCategory != null)
                {
                    var evt5 = new Event
                    {
                        Name = "Gala ESSCA 2024",
                        Description = "Soirée de gala annuelle de l'ESSCA.",
                        Code = "EXPOART",
                        Date = DateTime.Now.AddDays(7).AddHours(10).AddMinutes(0),
                        VenueId = esscaVenue.Id,
                        OrganizerId = organizer.Id,
                        CategoryId = culturesCategory.Id,
                        IsActive = true,
                        IsSubmitted = true,
                        PosterUrl = "https://images.unsplash.com/photo-1541961017774-22349e4a1262?w=800&h=600&fit=crop&q=80"
                    };
                    events.Add(evt5);
                }

                context.Events.AddRange(events);
                await context.SaveChangesAsync();

                // Seed Ticket Types and Seats for each event
                foreach (var evt in events)
                {
                    var venue = venuesList.FirstOrDefault(v => v.Id == evt.VenueId);
                    if (venue == null) continue;

                    var ticketTypes = new List<TicketType>();
                    var seats = new List<Seat>();

                    // Create ticket types based on event type
                    if (evt.Name.Contains("MISENGY") || evt.Name.Contains("Rock"))
                    {
                        // Concert events - 3 tiers
                        ticketTypes.AddRange(new[]
                        {
                            new TicketType { Name = "Bronze", Price = 40000, TotalCapacity = 50, Color = "#CD7F32", EventId = evt.Id },
                            new TicketType { Name = "Silver", Price = 50000, TotalCapacity = 50, Color = "#C0C0C0", EventId = evt.Id },
                            new TicketType { Name = "Gold", Price = 60000, TotalCapacity = 50, Color = "#FFD700", EventId = evt.Id }
                        });
                    }
                    else if (evt.Name.Contains("Festival"))
                    {
                        // Festival - 4 tiers
                        ticketTypes.AddRange(new[]
                        {
                            new TicketType { Name = "Standard", Price = 30000, TotalCapacity = 100, Color = "#4A90E2", EventId = evt.Id },
                            new TicketType { Name = "VIP", Price = 50000, TotalCapacity = 50, Color = "#9B59B6", EventId = evt.Id },
                            new TicketType { Name = "Premium", Price = 70000, TotalCapacity = 30, Color = "#E67E22", EventId = evt.Id },
                            new TicketType { Name = "Platinum", Price = 100000, TotalCapacity = 20, Color = "#1ABC9C", EventId = evt.Id }
                        });
                    }
                    else if (evt.Name.Contains("Séminaire"))
                    {
                        // Seminar - 2 tiers
                        ticketTypes.AddRange(new[]
                        {
                            new TicketType { Name = "Standard", Price = 50000, TotalCapacity = 150, Color = "#3498DB", EventId = evt.Id },
                            new TicketType { Name = "VIP", Price = 100000, TotalCapacity = 50, Color = "#E74C3C", EventId = evt.Id }
                        });
                    }
                    else
                    {
                        // Default - 2 tiers
                        ticketTypes.AddRange(new[]
                        {
                            new TicketType { Name = "Standard", Price = 25000, TotalCapacity = 100, Color = "#3498DB", EventId = evt.Id },
                            new TicketType { Name = "Premium", Price = 45000, TotalCapacity = 50, Color = "#E74C3C", EventId = evt.Id }
                        });
                    }

                    context.TicketTypes.AddRange(ticketTypes);
                    await context.SaveChangesAsync();

                    // Generate seats based on venue capacity
                    var totalRows = venue.TotalRows;
                    var totalCols = venue.TotalColumns;
                    var ticketTypeCount = ticketTypes.Count;
                    var rowsPerTier = totalRows / ticketTypeCount;

                    // Parse LayoutJson to find non-assignable zones
                    var layoutZones = new List<LayoutZoneItem>();
                    try {
                        layoutZones = System.Text.Json.JsonSerializer.Deserialize<List<LayoutZoneItem>>(venue.LayoutJson) ?? new List<LayoutZoneItem>();
                    } catch { }

                    for (int tier = 0; tier < ticketTypeCount; tier++)
                    {
                        var startRow = tier * rowsPerTier + 1;
                        var endRow = (tier == ticketTypeCount - 1) ? totalRows : (tier + 1) * rowsPerTier;
                        var ticketType = ticketTypes[tier];

                        for (int r = startRow; r <= endRow; r++)
                        {
                            for (int c = 1; c <= totalCols; c++)
                            {
                                // Check if this coordinate is in a void or stage zone (new format: exact match r, c)
                                bool isNonAssignable = layoutZones.Any(zone => zone.r == r && zone.c == c && (zone.type == "void" || zone.type == "stage"));
                                
                                if (isNonAssignable) continue;

                                var seatCode = $"{GetRowLetter(r)}{c:D2}";
                                seats.Add(new Seat 
                                { 
                                    Code = seatCode,
                                    PosX = r, 
                                    PosY = c, 
                                    Status = SeatStatus.Free, 
                                    TicketTypeId = ticketType.Id 
                                });
                            }
                        }
                    }

                    context.Seats.AddRange(seats);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static string GetCategoryDescription(string categoryName)
        {
            return categoryName switch
            {
                "Cultures" => "Événements culturels",
                "Spectacles et concerts" => "Spectacles et concerts",
                "Foires et séminaires" => "Foires et séminaires",
                "Autre" => "Autres types d'événements",
                _ => $"Catégorie {categoryName}"
            };
        }

        private static string GetRowLetter(int rowNumber)
        {
            return ((char)('A' + rowNumber - 1)).ToString();
        }

        private static string CreateSceneLayoutJson(List<(int r, int c, string type, string label)> zones)
        {
            var layoutItems = zones.Select(zone => new
            {
                r = zone.r,
                c = zone.c,
                type = zone.type,
                label = zone.label
            }).ToList();

            return System.Text.Json.JsonSerializer.Serialize(layoutItems);
        }

        private class LayoutZoneItem
        {
            public int r { get; set; }
            public int c { get; set; }
            public string type { get; set; } = string.Empty;
            public string? label { get; set; }
        }
    }
}

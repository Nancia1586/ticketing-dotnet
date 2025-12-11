using Ticketing.Core.Models;

namespace Ticketing.Core.Data
{
    public static class DbInitializer
    {
        public static void Initialize(TicketingDbContext context)
        {
            context.Database.EnsureCreated();

            // Look for any events.
            if (context.Events.Any())
            {
                return;   // DB has been seeded
            }

            var organizer = new Organizer
            {
                Name = "Passion Events",
                OrganizationName = "PASSION EVN3",
                Email = "contact@passion-events.mg",
                Password = "Password123!" // In real app, hash this
            };
            context.Organizers.Add(organizer);
            context.SaveChanges();

            var venue = new Venue
            {
                Name = "La City Ivandry",
                Address = "Ivandry, Antananarivo, Madagascar",
                TotalRows = 10,
                TotalColumns = 15, // Simplified for demo
                LayoutJson = "{}" // Placeholder, logic uses Row/Col
            };
            context.Venues.Add(venue);
            context.SaveChanges();

            var evt = new Event
            {
                Name = "MISENGY TROPICAL NIGHT",
                Description = "Une nuit inoubliable avec des rythmes tropicaux et une ambiance de folie. Venez vibrer avec nous !",
                Date = DateTime.Now.AddDays(14).AddHours(20).AddMinutes(30), // 2 weeks from now, 20:30
                VenueId = venue.Id,
                OrganizerId = organizer.Id,
                IsActive = true,
                PosterUrl = "https://placehold.co/600x400/1a1a1a/FFF?text=MISENGY+TROPICAL+NIGHT"
            };
            context.Events.Add(evt);
            context.SaveChanges();

            var ticketTypes = new List<TicketType>
            {
                new TicketType { Name = "Bronze", Price = 40000, TotalCapacity = 50, Color = "#CD7F32", EventId = evt.Id },
                new TicketType { Name = "Silver", Price = 50000, TotalCapacity = 50, Color = "#C0C0C0", EventId = evt.Id },
                new TicketType { Name = "Gold", Price = 60000, TotalCapacity = 50, Color = "#FFD700", EventId = evt.Id }
            };
            context.TicketTypes.AddRange(ticketTypes);
            context.SaveChanges();
            
            // Seed inventory seats
            var seats = new List<Seat>();

            // Gold - Front 3 rows
            for (int r = 1; r <= 3; r++)
            {
                for (int c = 1; c <= 14; c++)
                {
                    seats.Add(new Seat { PosX = r, PosY = c, Status = SeatStatus.Free, TicketTypeId = ticketTypes[2].Id });
                }
            }

            // Silver - Next 3 rows
            for (int r = 4; r <= 6; r++)
            {
                for (int c = 1; c <= 14; c++)
                {
                    seats.Add(new Seat { PosX = r, PosY = c, Status = SeatStatus.Free, TicketTypeId = ticketTypes[1].Id });
                }
            }

            // Bronze - Last 4 rows
            for (int r = 7; r <= 10; r++)
            {
                for (int c = 1; c <= 14; c++)
                {
                    seats.Add(new Seat { PosX = r, PosY = c, Status = SeatStatus.Free, TicketTypeId = ticketTypes[0].Id });
                }
            }
            context.Seats.AddRange(seats);
            context.SaveChanges();
        }
    }
}

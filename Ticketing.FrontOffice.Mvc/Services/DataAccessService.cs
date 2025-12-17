using System.Data;
using Microsoft.Data.SqlClient;
using Ticketing.Core.Models;

namespace Ticketing.FrontOffice.Mvc.Services
{
    public class DataAccessService
    {
        private readonly string _connectionString;

        public DataAccessService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // Events
        public async Task<List<Event>> GetActiveEventsAsync(string? searchTerm = null, DateTime? filterDate = null)
        {
            var events = new List<Event>();
            var query = @"
                SELECT e.Id, e.Name, e.Description, e.VenueId, e.OrganizerId, e.Date, e.PosterUrl, e.IsActive, e.CategoryId,
                       v.Id as Venue_Id, v.Name as Venue_Name, v.Address as Venue_Address, 
                       v.TotalRows as Venue_TotalRows, v.TotalColumns as Venue_TotalColumns, v.LayoutJson as Venue_LayoutJson,
                       c.Id as Category_Id, c.Name as Category_Name, c.Description as Category_Description, c.IsActive as Category_IsActive
                FROM Events e
                LEFT JOIN Venues v ON e.VenueId = v.Id
                LEFT JOIN Categories c ON e.CategoryId = c.Id
                WHERE e.IsActive = 1";

            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (e.Name LIKE @SearchTerm OR e.Description LIKE @SearchTerm)";
                parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            if (filterDate.HasValue)
            {
                query += " AND CAST(e.Date AS DATE) = @FilterDate";
                parameters.Add(new SqlParameter("@FilterDate", filterDate.Value.Date));
            }

            query += " ORDER BY e.Date";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddRange(parameters.ToArray());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var evt = new Event
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Description = reader.GetString("Description"),
                    VenueId = reader.GetInt32("VenueId"),
                    Date = reader.GetDateTime("Date"),
                    IsActive = reader.GetBoolean("IsActive"),
                    PosterUrl = reader.IsDBNull("PosterUrl") ? null : reader.GetString("PosterUrl"),
                    OrganizerId = reader.IsDBNull("OrganizerId") ? null : reader.GetInt32("OrganizerId"),
                    CategoryId = reader.IsDBNull("CategoryId") ? 0 : reader.GetInt32("CategoryId")
                };

                if (!reader.IsDBNull("Venue_Id"))
                {
                    evt.Venue = new Venue
                    {
                        Id = reader.GetInt32("Venue_Id"),
                        Name = reader.GetString("Venue_Name"),
                        Address = reader.GetString("Venue_Address"),
                        TotalRows = reader.GetInt32("Venue_TotalRows"),
                        TotalColumns = reader.GetInt32("Venue_TotalColumns"),
                        LayoutJson = reader.IsDBNull("Venue_LayoutJson") ? "[]" : reader.GetString("Venue_LayoutJson")
                    };
                }

                if (!reader.IsDBNull("Category_Id"))
                {
                    var categoryDescription = reader.IsDBNull("Category_Description") ? null : reader.GetString("Category_Description");
                    evt.Category = new Category
                    {
                        Id = reader.GetInt32("Category_Id"),
                        Name = reader.GetString("Category_Name"),
                        Description = categoryDescription ?? string.Empty,
                        IsActive = reader.GetBoolean("Category_IsActive")
                    };
                }

                events.Add(evt);
            }

            // Close the reader before loading TicketTypes
            reader.Close();

            // Load TicketTypes for all events
            foreach (var evt in events)
            {
                evt.TicketTypes = await GetTicketTypesByEventIdAsync(evt.Id, connection);
            }

            return events;
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            var query = @"
                SELECT e.Id, e.Name, e.Description, e.VenueId, e.OrganizerId, e.Date, e.PosterUrl, e.IsActive, e.CategoryId,
                       v.Id as Venue_Id, v.Name as Venue_Name, v.Address as Venue_Address, 
                       v.TotalRows as Venue_TotalRows, v.TotalColumns as Venue_TotalColumns, v.LayoutJson as Venue_LayoutJson,
                       c.Id as Category_Id, c.Name as Category_Name, c.Description as Category_Description, c.IsActive as Category_IsActive
                FROM Events e
                LEFT JOIN Venues v ON e.VenueId = v.Id
                LEFT JOIN Categories c ON e.CategoryId = c.Id
                WHERE e.Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var evt = new Event
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Description = reader.GetString("Description"),
                    VenueId = reader.GetInt32("VenueId"),
                    Date = reader.GetDateTime("Date"),
                    IsActive = reader.GetBoolean("IsActive"),
                    PosterUrl = reader.IsDBNull("PosterUrl") ? null : reader.GetString("PosterUrl"),
                    OrganizerId = reader.IsDBNull("OrganizerId") ? null : reader.GetInt32("OrganizerId")
                };

                if (!reader.IsDBNull("Venue_Id"))
                {
                    evt.Venue = new Venue
                    {
                        Id = reader.GetInt32("Venue_Id"),
                        Name = reader.GetString("Venue_Name"),
                        Address = reader.GetString("Venue_Address"),
                        TotalRows = reader.GetInt32("Venue_TotalRows"),
                        TotalColumns = reader.GetInt32("Venue_TotalColumns"),
                        LayoutJson = reader.IsDBNull("Venue_LayoutJson") ? "[]" : reader.GetString("Venue_LayoutJson")
                    };
                }

                // Load TicketTypes
                evt.TicketTypes = await GetTicketTypesByEventIdAsync(id, connection);

                // Load Reservations with Seats
                evt.Reservations = await GetReservationsByEventIdAsync(id, connection);

                return evt;
            }

            return null;
        }

        // TicketTypes
        public async Task<TicketType?> GetTicketTypeByIdAsync(int id)
        {
            var query = @"
                SELECT Id, Name, Price, TotalCapacity, Color, IsReservedSeating, EventId
                FROM TicketTypes
                WHERE Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TicketType
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Price = reader.GetDecimal("Price"),
                    TotalCapacity = reader.GetInt32("TotalCapacity"),
                    Color = reader.GetString("Color"),
                    IsReservedSeating = reader.GetBoolean("IsReservedSeating"),
                    EventId = reader.GetInt32("EventId")
                };
            }

            return null;
        }

        private async Task<List<TicketType>> GetTicketTypesByEventIdAsync(int eventId, SqlConnection connection)
        {
            var ticketTypes = new List<TicketType>();
            var query = @"
                SELECT Id, Name, Price, TotalCapacity, Color, IsReservedSeating, EventId
                FROM TicketTypes
                WHERE EventId = @EventId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@EventId", eventId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ticketType = new TicketType
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Price = reader.GetDecimal("Price"),
                    TotalCapacity = reader.GetInt32("TotalCapacity"),
                    Color = reader.GetString("Color"),
                    IsReservedSeating = reader.GetBoolean("IsReservedSeating"),
                    EventId = reader.GetInt32("EventId")
                };

                ticketTypes.Add(ticketType);
            }

            // Load Seats for all ticket types after closing the reader
            foreach (var ticketType in ticketTypes)
            {
                ticketType.Seats = await GetSeatsByTicketTypeIdAsync(ticketType.Id, connection);
            }

            return ticketTypes;
        }

        // Seats
        private async Task<List<Seat>> GetSeatsByTicketTypeIdAsync(int ticketTypeId, SqlConnection connection)
        {
            var seats = new List<Seat>();
            var query = @"
                SELECT Id, Code, PosX, PosY, Status, TicketTypeId, ReservationId
                FROM Seats
                WHERE TicketTypeId = @TicketTypeId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@TicketTypeId", ticketTypeId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                seats.Add(new Seat
                {
                    Id = reader.GetInt32("Id"),
                    Code = reader.GetString("Code"),
                    PosX = reader.GetInt32("PosX"),
                    PosY = reader.GetInt32("PosY"),
                    Status = (SeatStatus)reader.GetInt32("Status"),
                    TicketTypeId = reader.GetInt32("TicketTypeId"),
                    ReservationId = reader.IsDBNull("ReservationId") ? null : reader.GetInt32("ReservationId")
                });
            }

            return seats;
        }

        // Reservations
        public async Task<List<Reservation>> GetReservationsByEmailAsync(string email)
        {
            var reservations = new List<Reservation>();
            var query = @"
                SELECT r.Id, r.CustomerName, r.PhoneNumber, r.Email, r.SeatCount, r.Status, 
                       r.ReservationDate, r.TotalAmount, r.EventId,
                       e.Id as Event_Id, e.Name as Event_Name, e.Description as Event_Description,
                       e.VenueId as Event_VenueId, e.Date as Event_Date, e.PosterUrl as Event_PosterUrl, e.IsActive as Event_IsActive
                FROM Reservations r
                LEFT JOIN Events e ON r.EventId = e.Id
                WHERE r.Email = @Email
                ORDER BY r.ReservationDate DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Email", email));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var reservation = new Reservation
                {
                    Id = reader.GetInt32("Id"),
                    CustomerName = reader.GetString("CustomerName"),
                    PhoneNumber = reader.GetString("PhoneNumber"),
                    Email = reader.GetString("Email"),
                    SeatCount = reader.GetInt32("SeatCount"),
                    Status = (ReservationStatus)reader.GetInt32("Status"),
                    ReservationDate = reader.GetDateTime("ReservationDate"),
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    EventId = reader.GetInt32("EventId")
                };

                if (!reader.IsDBNull("Event_Id"))
                {
                    reservation.Event = new Event
                    {
                        Id = reader.GetInt32("Event_Id"),
                        Name = reader.GetString("Event_Name"),
                        Description = reader.GetString("Event_Description"),
                        VenueId = reader.GetInt32("Event_VenueId"),
                        Date = reader.GetDateTime("Event_Date"),
                        IsActive = reader.GetBoolean("Event_IsActive"),
                        PosterUrl = reader.IsDBNull("Event_PosterUrl") ? null : reader.GetString("Event_PosterUrl")
                    };
                }

                // Load Seats for this reservation
                reservation.Seats = await GetSeatsByReservationIdAsync(reservation.Id, connection);
                reservations.Add(reservation);
            }

            return reservations;
        }

        private async Task<List<Reservation>> GetReservationsByEventIdAsync(int eventId, SqlConnection connection)
        {
            var reservations = new List<Reservation>();
            var query = @"
                SELECT Id, CustomerName, PhoneNumber, Email, SeatCount, Status, ReservationDate, TotalAmount, EventId
                FROM Reservations
                WHERE EventId = @EventId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@EventId", eventId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var reservation = new Reservation
                {
                    Id = reader.GetInt32("Id"),
                    CustomerName = reader.GetString("CustomerName"),
                    PhoneNumber = reader.GetString("PhoneNumber"),
                    Email = reader.GetString("Email"),
                    SeatCount = reader.GetInt32("SeatCount"),
                    Status = (ReservationStatus)reader.GetInt32("Status"),
                    ReservationDate = reader.GetDateTime("ReservationDate"),
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    EventId = reader.GetInt32("EventId")
                };

                // Load Seats for this reservation
                reservation.Seats = await GetSeatsByReservationIdAsync(reservation.Id, connection);
                reservations.Add(reservation);
            }

            return reservations;
        }

        private async Task<List<Seat>> GetSeatsByReservationIdAsync(int reservationId, SqlConnection connection)
        {
            var seats = new List<Seat>();
            var query = @"
                SELECT Id, Code, PosX, PosY, Status, TicketTypeId, ReservationId
                FROM Seats
                WHERE ReservationId = @ReservationId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@ReservationId", reservationId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                seats.Add(new Seat
                {
                    Id = reader.GetInt32("Id"),
                    Code = reader.GetString("Code"),
                    PosX = reader.GetInt32("PosX"),
                    PosY = reader.GetInt32("PosY"),
                    Status = (SeatStatus)reader.GetInt32("Status"),
                    TicketTypeId = reader.GetInt32("TicketTypeId"),
                    ReservationId = reader.GetInt32("ReservationId")
                });
            }

            return seats;
        }

        public async Task<int> CreateReservationAsync(Reservation reservation)
        {
            var query = @"
                INSERT INTO Reservations (CustomerName, PhoneNumber, Email, SeatCount, Status, ReservationDate, TotalAmount, EventId)
                OUTPUT INSERTED.Id
                VALUES (@CustomerName, @PhoneNumber, @Email, @SeatCount, @Status, @ReservationDate, @TotalAmount, @EventId)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@CustomerName", reservation.CustomerName));
            command.Parameters.Add(new SqlParameter("@PhoneNumber", reservation.PhoneNumber));
            command.Parameters.Add(new SqlParameter("@Email", reservation.Email));
            command.Parameters.Add(new SqlParameter("@SeatCount", reservation.SeatCount));
            command.Parameters.Add(new SqlParameter("@Status", (int)reservation.Status));
            command.Parameters.Add(new SqlParameter("@ReservationDate", reservation.ReservationDate));
            command.Parameters.Add(new SqlParameter("@TotalAmount", reservation.TotalAmount));
            command.Parameters.Add(new SqlParameter("@EventId", reservation.EventId));

            var result = await command.ExecuteScalarAsync();
            if (result == null)
                throw new InvalidOperationException("Failed to create reservation.");
            var reservationId = (int)result;

            // Insert Seats
            if (reservation.Seats.Any())
            {
                await InsertSeatsAsync(reservation.Seats, reservationId, connection);
            }

            return reservationId;
        }

        private async Task InsertSeatsAsync(ICollection<Seat> seats, int reservationId, SqlConnection connection)
        {
            var query = @"
                INSERT INTO Seats (Code, PosX, PosY, Status, TicketTypeId, ReservationId)
                VALUES (@Code, @PosX, @PosY, @Status, @TicketTypeId, @ReservationId)";

            foreach (var seat in seats)
            {
                using var command = new SqlCommand(query, connection);
                command.Parameters.Add(new SqlParameter("@Code", seat.Code));
                command.Parameters.Add(new SqlParameter("@PosX", seat.PosX));
                command.Parameters.Add(new SqlParameter("@PosY", seat.PosY));
                command.Parameters.Add(new SqlParameter("@Status", (int)seat.Status));
                command.Parameters.Add(new SqlParameter("@TicketTypeId", seat.TicketTypeId));
                command.Parameters.Add(new SqlParameter("@ReservationId", reservationId));

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            var query = @"
                SELECT r.Id, r.CustomerName, r.PhoneNumber, r.Email, r.SeatCount, r.Status, 
                       r.ReservationDate, r.TotalAmount, r.EventId,
                       e.Id as Event_Id, e.Name as Event_Name, e.Description as Event_Description,
                       e.VenueId as Event_VenueId, e.Date as Event_Date, e.PosterUrl as Event_PosterUrl, e.IsActive as Event_IsActive
                FROM Reservations r
                LEFT JOIN Events e ON r.EventId = e.Id
                WHERE r.Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var reservation = new Reservation
                {
                    Id = reader.GetInt32("Id"),
                    CustomerName = reader.GetString("CustomerName"),
                    PhoneNumber = reader.GetString("PhoneNumber"),
                    Email = reader.GetString("Email"),
                    SeatCount = reader.GetInt32("SeatCount"),
                    Status = (ReservationStatus)reader.GetInt32("Status"),
                    ReservationDate = reader.GetDateTime("ReservationDate"),
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    EventId = reader.GetInt32("EventId")
                };

                if (!reader.IsDBNull("Event_Id"))
                {
                    reservation.Event = new Event
                    {
                        Id = reader.GetInt32("Event_Id"),
                        Name = reader.GetString("Event_Name"),
                        Description = reader.GetString("Event_Description"),
                        VenueId = reader.GetInt32("Event_VenueId"),
                        Date = reader.GetDateTime("Event_Date"),
                        IsActive = reader.GetBoolean("Event_IsActive"),
                        PosterUrl = reader.IsDBNull("Event_PosterUrl") ? null : reader.GetString("Event_PosterUrl")
                    };
                }

                // Load Seats
                reservation.Seats = await GetSeatsByReservationIdAsync(reservation.Id, connection);

                return reservation;
            }

            return null;
        }

        // Organizers
        public async Task<int> CreateOrganizerAsync(Organizer organizer)
        {
            var query = @"
                INSERT INTO Organizers (Name, Email, OrganizationName, Password)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Email, @OrganizationName, @Password)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Name", organizer.Name));
            command.Parameters.Add(new SqlParameter("@Email", organizer.Email));
            command.Parameters.Add(new SqlParameter("@OrganizationName", organizer.OrganizationName));
            command.Parameters.Add(new SqlParameter("@Password", organizer.Password));

            var result = await command.ExecuteScalarAsync();
            if (result == null)
                throw new InvalidOperationException("Failed to create organizer.");
            return (int)result;
        }
    }
}


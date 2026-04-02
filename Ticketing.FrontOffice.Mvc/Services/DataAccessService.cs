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

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var categories = new List<Category>();
            var query = @"
                SELECT Id, Name, Description, IsActive
                FROM Categories
                WHERE IsActive = 1
                ORDER BY Name";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Description = reader.IsDBNull("Description") ? string.Empty : reader.GetString("Description"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }

            return categories;
        }

        public async Task<List<Event>> GetActiveEventsAsync(string? searchTerm = null, DateTime? filterDate = null, int? categoryId = null)
        {
            var events = new List<Event>();
            // Inline MIN(price) subquery — listing page only needs the minimum price, not all seats
            var query = @"
                SELECT e.Id, e.Name, e.Description, e.VenueId, e.OrganizerId, e.Date, e.PosterUrl, e.IsActive, e.CategoryId,
                       v.Name as Venue_Name,
                       c.Id as Category_Id, c.Name as Category_Name, c.IsActive as Category_IsActive,
                       (SELECT MIN(tt.Price) FROM TicketTypes tt WHERE tt.EventId = e.Id) as MinPrice
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

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query += " AND e.CategoryId = @CategoryId";
                parameters.Add(new SqlParameter("@CategoryId", categoryId.Value));
            }

            query += " ORDER BY e.Date";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddRange(parameters.ToArray());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var minPrice = reader.IsDBNull("MinPrice") ? 0m : reader.GetDecimal("MinPrice");

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
                    CategoryId = reader.IsDBNull("CategoryId") ? 0 : reader.GetInt32("CategoryId"),
                    // Populate a single TicketType entry carrying MinPrice so the view's .Min(t => t.Price) works
                    TicketTypes = minPrice > 0
                        ? new List<TicketType> { new TicketType { Price = minPrice } }
                        : new List<TicketType>()
                };

                if (!reader.IsDBNull("Venue_Name"))
                    evt.Venue = new Venue { Name = reader.GetString("Venue_Name") };

                if (!reader.IsDBNull("Category_Id"))
                {
                    evt.Category = new Category
                    {
                        Id = reader.GetInt32("Category_Id"),
                        Name = reader.GetString("Category_Name"),
                        IsActive = reader.GetBoolean("Category_IsActive")
                    };
                }

                events.Add(evt);
            }

            return events;
        }

        public async Task<List<Event>> GetSimilarEventsAsync(int excludeEventId, int categoryId, int count = 4)
        {
            var events = new List<Event>();
            var query = @"
                SELECT TOP (@Count) e.Id, e.Name, e.Date, e.PosterUrl, e.CategoryId,
                       v.Name as Venue_Name
                FROM Events e
                LEFT JOIN Venues v ON e.VenueId = v.Id
                WHERE e.IsActive = 1 AND e.Id <> @ExcludeId
                  AND (e.CategoryId = @CategoryId OR @CategoryId = 0)
                ORDER BY e.Date";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Count", count));
            command.Parameters.Add(new SqlParameter("@ExcludeId", excludeEventId));
            command.Parameters.Add(new SqlParameter("@CategoryId", categoryId));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var evt = new Event
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Date = reader.GetDateTime("Date"),
                    PosterUrl = reader.IsDBNull("PosterUrl") ? null : reader.GetString("PosterUrl"),
                    CategoryId = reader.GetInt32("CategoryId")
                };
                if (!reader.IsDBNull("Venue_Name"))
                {
                    evt.Venue = new Venue { Name = reader.GetString("Venue_Name") };
                }
                events.Add(evt);
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
                    CategoryId = reader.GetInt32("CategoryId"),
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

                if (!reader.IsDBNull("Category_Id"))
                {
                    evt.Category = new Category
                    {
                        Id = reader.GetInt32("Category_Id"),
                        Name = reader.GetString("Category_Name"),
                        Description = reader.IsDBNull("Category_Description") ? null : reader.GetString("Category_Description"),
                        IsActive = reader.GetBoolean("Category_IsActive")
                    };
                }

                if (evt.OrganizerId.HasValue)
                {
                    var organizerQuery = "SELECT Id, Name, Email, OrganizationName FROM Organizers WHERE Id = @OrganizerId";
                    using var organizerCommand = new SqlCommand(organizerQuery, connection);
                    organizerCommand.Parameters.Add(new SqlParameter("@OrganizerId", evt.OrganizerId.Value));
                    using var organizerReader = await organizerCommand.ExecuteReaderAsync();
                    if (await organizerReader.ReadAsync())
                    {
                        evt.Organizer = new Organizer
                        {
                            Id = organizerReader.GetInt32("Id"),
                            Name = organizerReader.GetString("Name"),
                            Email = organizerReader.GetString("Email"),
                            OrganizationName = organizerReader.IsDBNull("OrganizationName") ? null : organizerReader.GetString("OrganizationName")
                        };
                    }
                    organizerReader.Close();
                }

                evt.TicketTypes = await GetTicketTypesByEventIdAsync(id, connection);

                return evt;
            }

            return null;
        }

        // TicketTypes
        public async Task<TicketType?> GetTicketTypeByIdAsync(int id)
        {
            var query = @"
                SELECT Id, Name, Price, TotalCapacity, Color, EventId
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
                    EventId = reader.GetInt32("EventId")
                };
            }

            return null;
        }

        private async Task<List<TicketType>> GetTicketTypesByEventIdAsync(int eventId, SqlConnection connection)
        {
            var ticketTypes = new List<TicketType>();
            var query = @"
                SELECT Id, Name, Price, TotalCapacity, Color, EventId
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
                    EventId = reader.GetInt32("EventId")
                };

                ticketTypes.Add(ticketType);
            }
            reader.Close();

            // Load all seats for all ticket types in one query (fix N+1)
            if (ticketTypes.Any())
            {
                var ticketTypeIds = ticketTypes.Select(tt => tt.Id).ToList();
                var seatsQuery = @"
                    SELECT Id, Code, PosX, PosY, Status, TicketTypeId, ReservationId
                    FROM Seats
                    WHERE TicketTypeId IN ({0})";

                var ticketTypeIdParams = string.Join(",", ticketTypeIds.Select((_, i) => $"@TicketTypeId{i}"));
                seatsQuery = string.Format(seatsQuery, ticketTypeIdParams);

                using var seatsCommand = new SqlCommand(seatsQuery, connection);
                for (int i = 0; i < ticketTypeIds.Count; i++)
                {
                    seatsCommand.Parameters.Add(new SqlParameter($"@TicketTypeId{i}", ticketTypeIds[i]));
                }

                var seatsByTicketTypeId = new Dictionary<int, List<Seat>>();
                foreach (var ticketTypeId in ticketTypeIds)
                {
                    seatsByTicketTypeId[ticketTypeId] = new List<Seat>();
                }

                using var seatsReader = await seatsCommand.ExecuteReaderAsync();
                while (await seatsReader.ReadAsync())
                {
                    var ticketTypeId = seatsReader.GetInt32("TicketTypeId");
                    var seat = new Seat
                    {
                        Id = seatsReader.GetInt32("Id"),
                        Code = seatsReader.GetString("Code"),
                        PosX = seatsReader.GetInt32("PosX"),
                        PosY = seatsReader.GetInt32("PosY"),
                        Status = (SeatStatus)seatsReader.GetInt32("Status"),
                        TicketTypeId = ticketTypeId,
                        ReservationId = seatsReader.IsDBNull("ReservationId") ? null : seatsReader.GetInt32("ReservationId")
                    };

                    if (seatsByTicketTypeId.ContainsKey(ticketTypeId))
                    {
                        seatsByTicketTypeId[ticketTypeId].Add(seat);
                    }
                }
                seatsReader.Close();

                // Assign seats to ticket types
                foreach (var ticketType in ticketTypes)
                {
                    if (seatsByTicketTypeId.ContainsKey(ticketType.Id))
                    {
                        ticketType.Seats = seatsByTicketTypeId[ticketType.Id];
                    }
                    else
                    {
                        ticketType.Seats = new List<Seat>();
                    }
                }
            }

            return ticketTypes;
        }

        // Seats - This method is now only used for single ticket type queries
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
            reader.Close();

            return seats;
        }

        public async Task<List<Reservation>> GetReservationsByEmailAsync(string email)
        {
            var reservations = new List<Reservation>();
            var query = @"
                SELECT r.Id, r.CustomerName, r.PhoneNumber, r.Email, r.SeatCount, r.Status,
                       r.ReservationDate, r.TotalAmount, r.EventId, r.PaymentReference, r.NotificationToken, r.Reference,
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
                    EventId = reader.GetInt32("EventId"),
                    PaymentReference = reader.IsDBNull("PaymentReference") ? null : reader.GetString("PaymentReference"),
                    NotificationToken = reader.IsDBNull("NotificationToken") ? null : reader.GetString("NotificationToken")
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

                reservations.Add(reservation);
            }
            reader.Close();

            // Batch load all seats in one query instead of N+1 queries
            if (reservations.Any())
            {
                var seatsByReservationId = await GetSeatsByReservationIdsAsync(
                    reservations.Select(r => r.Id).ToList(), connection);
                foreach (var reservation in reservations)
                {
                    reservation.Seats = seatsByReservationId.TryGetValue(reservation.Id, out var seats)
                        ? seats : new List<Seat>();
                }
            }

            return reservations;
        }

        private async Task<List<Reservation>> GetReservationsByEventIdAsync(int eventId, SqlConnection connection)
        {
            var reservations = new List<Reservation>();
            var query = @"
                SELECT Id, CustomerName, PhoneNumber, Email, SeatCount, Status, ReservationDate, TotalAmount, EventId, PaymentReference, NotificationToken, Reference
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
                    EventId = reader.GetInt32("EventId"),
                    PaymentReference = reader.IsDBNull("PaymentReference") ? null : reader.GetString("PaymentReference"),
                    NotificationToken = reader.IsDBNull("NotificationToken") ? null : reader.GetString("NotificationToken"),
                    Reference = reader.IsDBNull("Reference") ? string.Empty : reader.GetString("Reference")
                };

                reservations.Add(reservation);
            }
            reader.Close();

            // Batch load all seats in one query instead of N+1 queries
            if (reservations.Any())
            {
                var seatsByReservationId = await GetSeatsByReservationIdsAsync(
                    reservations.Select(r => r.Id).ToList(), connection);
                foreach (var reservation in reservations)
                {
                    reservation.Seats = seatsByReservationId.TryGetValue(reservation.Id, out var seats)
                        ? seats : new List<Seat>();
                }
            }

            return reservations;
        }

        private async Task<List<Seat>> GetSeatsByReservationIdAsync(int reservationId, SqlConnection connection)
        {
            var result = await GetSeatsByReservationIdsAsync(new List<int> { reservationId }, connection);
            return result.TryGetValue(reservationId, out var seats) ? seats : new List<Seat>();
        }

        private async Task<Dictionary<int, List<Seat>>> GetSeatsByReservationIdsAsync(List<int> reservationIds, SqlConnection connection)
        {
            var result = new Dictionary<int, List<Seat>>();
            if (!reservationIds.Any()) return result;

            foreach (var id in reservationIds) result[id] = new List<Seat>();

            var paramNames = string.Join(",", reservationIds.Select((_, i) => $"@RId{i}"));
            var query = $@"
                SELECT Id, Code, PosX, PosY, Status, TicketTypeId, ReservationId
                FROM Seats
                WHERE ReservationId IN ({paramNames})";

            using var command = new SqlCommand(query, connection);
            for (int i = 0; i < reservationIds.Count; i++)
                command.Parameters.Add(new SqlParameter($"@RId{i}", reservationIds[i]));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var rid = reader.GetInt32("ReservationId");
                result[rid].Add(new Seat
                {
                    Id = reader.GetInt32("Id"),
                    Code = reader.GetString("Code"),
                    PosX = reader.GetInt32("PosX"),
                    PosY = reader.GetInt32("PosY"),
                    Status = (SeatStatus)reader.GetInt32("Status"),
                    TicketTypeId = reader.GetInt32("TicketTypeId"),
                    ReservationId = rid
                });
            }

            return result;
        }

        public async Task<int> CreateReservationAsync(Reservation reservation)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Reuse the same connection for reference generation
            string reservationReference = await GenerateReservationReferenceAsync(reservation.EventId, connection);

            var query = @"
                INSERT INTO Reservations (CustomerName, PhoneNumber, Email, SeatCount, Status, ReservationDate, TotalAmount, EventId, PaymentMethod, PaymentReference, NotificationToken, Reference)
                OUTPUT INSERTED.Id
                VALUES (@CustomerName, @PhoneNumber, @Email, @SeatCount, @Status, @ReservationDate, @TotalAmount, @EventId, @PaymentMethod, @PaymentReference, @NotificationToken, @Reference)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@CustomerName", reservation.CustomerName));
            command.Parameters.Add(new SqlParameter("@PhoneNumber", reservation.PhoneNumber));
            command.Parameters.Add(new SqlParameter("@Email", reservation.Email));
            command.Parameters.Add(new SqlParameter("@SeatCount", reservation.SeatCount));
            command.Parameters.Add(new SqlParameter("@Status", (int)reservation.Status));
            command.Parameters.Add(new SqlParameter("@ReservationDate", reservation.ReservationDate));
            command.Parameters.Add(new SqlParameter("@TotalAmount", reservation.TotalAmount));
            command.Parameters.Add(new SqlParameter("@EventId", reservation.EventId));
            command.Parameters.Add(new SqlParameter("@PaymentMethod", reservation.PaymentMethod));
            command.Parameters.Add(new SqlParameter("@PaymentReference", (object?)reservation.PaymentReference ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@NotificationToken", (object?)reservation.NotificationToken ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@Reference", reservationReference));

            var result = await command.ExecuteScalarAsync();
            if (result == null)
                throw new InvalidOperationException("Failed to create reservation.");
            var reservationId = (int)result;
            
            reservation.Reference = reservationReference;

            if (reservation.Seats.Any())
            {
                await InsertSeatsAsync(reservation.Seats, reservationId, connection);
            }

            return reservationId;
        }

        private async Task<string> GenerateReservationReferenceAsync(int eventId, SqlConnection connection)
        {
            var eventQuery = "SELECT Code FROM Events WHERE Id = @EventId";
            using var eventCommand = new SqlCommand(eventQuery, connection);
            eventCommand.Parameters.Add(new SqlParameter("@EventId", eventId));

            var eventCode = await eventCommand.ExecuteScalarAsync() as string;
            if (string.IsNullOrEmpty(eventCode))
                eventCode = "EVT";

            var countQuery = @"
                SELECT COUNT(*)
                FROM Reservations
                WHERE EventId = @EventId AND Reference LIKE @Pattern";

            using var countCommand = new SqlCommand(countQuery, connection);
            countCommand.Parameters.Add(new SqlParameter("@EventId", eventId));
            countCommand.Parameters.Add(new SqlParameter("@Pattern", $"{eventCode}-%"));

            var count = (int)await countCommand.ExecuteScalarAsync();
            return $"{eventCode}-{count + 1:D3}";
        }

        private async Task InsertSeatsAsync(ICollection<Seat> seats, int reservationId, SqlConnection connection)
        {
            if (!seats.Any()) return;

            // Batch all seat inserts into a single SQL statement
            var valueClauses = new List<string>();
            using var command = new SqlCommand();
            command.Connection = connection;

            int i = 0;
            foreach (var seat in seats)
            {
                valueClauses.Add($"(@Code{i}, @PosX{i}, @PosY{i}, @Status{i}, @TicketTypeId{i}, @ReservationId{i})");
                command.Parameters.Add(new SqlParameter($"@Code{i}", seat.Code));
                command.Parameters.Add(new SqlParameter($"@PosX{i}", seat.PosX));
                command.Parameters.Add(new SqlParameter($"@PosY{i}", seat.PosY));
                command.Parameters.Add(new SqlParameter($"@Status{i}", (int)seat.Status));
                command.Parameters.Add(new SqlParameter($"@TicketTypeId{i}", seat.TicketTypeId));
                command.Parameters.Add(new SqlParameter($"@ReservationId{i}", reservationId));
                i++;
            }

            command.CommandText = $@"
                INSERT INTO Seats (Code, PosX, PosY, Status, TicketTypeId, ReservationId)
                VALUES {string.Join(", ", valueClauses)}";

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            var query = @"
                SELECT r.Id, r.CustomerName, r.PhoneNumber, r.Email, r.SeatCount, r.Status, 
                       r.ReservationDate, r.TotalAmount, r.EventId, r.PaymentReference, r.NotificationToken, r.Reference,
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
                    EventId = reader.GetInt32("EventId"),
                    PaymentReference = reader.IsDBNull("PaymentReference") ? null : reader.GetString("PaymentReference"),
                    NotificationToken = reader.IsDBNull("NotificationToken") ? null : reader.GetString("NotificationToken"),
                    Reference = reader.IsDBNull("Reference") ? string.Empty : reader.GetString("Reference")
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

                reservation.Seats = await GetSeatsByReservationIdAsync(reservation.Id, connection);

                return reservation;
            }

            return null;
        }

        public async Task UpdateReservationPaymentAsync(int reservationId, ReservationStatus status, string paymentReference, string? notificationToken)
        {
            var query = @"
                UPDATE Reservations 
                SET Status = @Status, PaymentReference = @PaymentReference, NotificationToken = @NotificationToken
                WHERE Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Id", reservationId));
            command.Parameters.Add(new SqlParameter("@Status", (int)status));
            command.Parameters.Add(new SqlParameter("@PaymentReference", paymentReference));
            command.Parameters.Add(new SqlParameter("@NotificationToken", (object?)notificationToken ?? DBNull.Value));

            await command.ExecuteNonQueryAsync();
        }

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


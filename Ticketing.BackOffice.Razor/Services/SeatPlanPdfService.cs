using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Ticketing.Core.Models;
using System.Text.Json;

namespace Ticketing.BackOffice.Razor.Services
{
    public class SeatPlanPdfService
    {
        public byte[] GenerateSeatPlanPdf(Event eventData)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text(eventData.Name).FontSize(20).Bold();
                            column.Item().Text($"Date: {eventData.Date:dd/MM/yyyy HH:mm}").FontSize(12);
                            if (eventData.Venue != null)
                            {
                                column.Item().Text($"Lieu: {eventData.Venue.Name}").FontSize(12);
                            }
                            column.Item().PaddingBottom(10);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            if (eventData.Venue != null && eventData.Venue.TotalRows > 0 && eventData.Venue.TotalColumns > 0)
                            {
                                column.Item().Element(container => RenderSeatGrid(container, eventData));
                            }
                            else
                            {
                                column.Item().Text("Aucun plan de sièges disponible pour cet événement.").FontSize(12);
                            }

                            column.Item().PaddingTop(20);
                            column.Item().Element(container => RenderLegend(container, eventData));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Généré le ").FontSize(8);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).Bold();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private void RenderSeatGrid(IContainer container, Event eventData)
        {
            var venue = eventData.Venue!;
            var totalRows = venue.TotalRows;
            var totalColumns = venue.TotalColumns;

            var seatMap = new Dictionary<(int row, int col), SeatStatus>();
            var ticketTypeMap = new Dictionary<(int row, int col), TicketType>();

            foreach (var ticketType in eventData.TicketTypes)
            {
                foreach (var seat in ticketType.Seats)
                {
                    var key = (seat.PosX, seat.PosY);
                    seatMap[key] = seat.Status;
                    ticketTypeMap[key] = ticketType;
                }
            }

            var layoutConfig = ParseLayoutJson(venue.LayoutJson);

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    for (int c = 1; c <= totalColumns; c++)
                    {
                        columns.ConstantColumn(25);
                    }
                });

                table.Header(header =>
                {
                    header.Cell().Element(cell => cell
                        .Border(1)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(2)
                        .Text(""));
                    
                    for (int c = 1; c <= totalColumns; c++)
                    {
                        header.Cell().Element(cell => cell
                            .Border(1)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(2)
                            .AlignCenter()
                            .Text(c.ToString()).FontSize(8).Bold());
                    }
                });

                for (int r = 1; r <= totalRows; r++)
                {
                    var rowLetter = (char)('A' + r - 1);
                    
                    table.Cell().Element(cell => cell
                        .Border(1)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(2)
                        .AlignCenter()
                        .Text(rowLetter.ToString()).FontSize(8).Bold());

                    for (int c = 1; c <= totalColumns; c++)
                    {
                        var key = (r, c);
                        var seatCode = $"{rowLetter}{c}";

                        if (layoutConfig.ContainsKey(key) && layoutConfig[key].type == "void")
                        {
                            table.Cell().Element(cell => cell
                                .Border(1)
                                .Background(Colors.Grey.Lighten4)
                                .Padding(2)
                                .AlignCenter()
                                .Text("").FontSize(6));
                        }
                        else if (seatMap.TryGetValue(key, out var status))
                        {
                            var ticketType = ticketTypeMap.GetValueOrDefault(key);
                            var color = GetStatusColor(status, ticketType?.Color);
                            
                            table.Cell().Element(cell => cell
                                .Border(1)
                                .Background(color)
                                .Padding(2)
                                .AlignCenter()
                                .Text(seatCode).FontSize(6)
                                .FontColor(GetContrastColor(color)));
                        }
                        else
                        {
                            table.Cell().Element(cell => cell
                                .Border(1)
                                .Background(Colors.White)
                                .Padding(2)
                                .AlignCenter()
                                .Text(seatCode).FontSize(6)
                                .FontColor(Colors.Grey.Lighten1));
                        }
                    }
                }
            });
        }

        private void RenderLegend(IContainer container, Event eventData)
        {
            container.Column(column =>
            {
                column.Item().PaddingBottom(5).Text("Légende").FontSize(12).Bold();

                column.Item().Row(row =>
                {
                    row.AutoItem().Element(cell => cell
                        .Width(15)
                        .Height(15)
                        .Background(Colors.Green.Lighten2)
                        .Border(1));
                    row.AutoItem().PaddingLeft(5).Text("Libre").FontSize(9);

                    row.AutoItem().PaddingLeft(15).Element(cell => cell
                        .Width(15)
                        .Height(15)
                        .Background(Colors.Orange.Lighten2)
                        .Border(1));
                    row.AutoItem().PaddingLeft(5).Text("Réservé").FontSize(9);

                    row.AutoItem().PaddingLeft(15).Element(cell => cell
                        .Width(15)
                        .Height(15)
                        .Background(Colors.Red.Lighten2)
                        .Border(1));
                    row.AutoItem().PaddingLeft(5).Text("Occupé").FontSize(9);

                    row.AutoItem().PaddingLeft(15).Element(cell => cell
                        .Width(15)
                        .Height(15)
                        .Background(Colors.Blue.Lighten2)
                        .Border(1));
                    row.AutoItem().PaddingLeft(5).Text("Maintenu").FontSize(9);
                });

                if (eventData.TicketTypes.Any())
                {
                    column.Item().PaddingTop(10);
                    column.Item().PaddingBottom(5).Text("Types de billets:").FontSize(10).Bold();

                    foreach (var ticketType in eventData.TicketTypes)
                    {
                        column.Item().Row(row =>
                        {
                            row.AutoItem().Element(cell => cell
                                .Width(15)
                                .Height(15)
                                .Background(ParseColor(ticketType.Color))
                                .Border(1));
                            row.AutoItem().PaddingLeft(5).Text($"{ticketType.Name} - {ticketType.Price:C}").FontSize(9);
                        });
                    }
                }
            });
        }

        private Dictionary<(int row, int col), GridCellConfig> ParseLayoutJson(string layoutJson)
        {
            var result = new Dictionary<(int row, int col), GridCellConfig>();
            
            if (string.IsNullOrEmpty(layoutJson))
                return result;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<List<GridCellConfig>>(layoutJson, options);
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        result[(item.r, item.c)] = item;
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private string GetStatusColor(SeatStatus status, string? ticketTypeColor = null)
        {
            if (status == SeatStatus.Free)
            {
                return Colors.Green.Lighten2;
            }
            
            if (status == SeatStatus.Reserved)
            {
                return Colors.Orange.Lighten2;
            }
            
            if (status == SeatStatus.Taken)
            {
                return Colors.Red.Lighten2;
            }
            
            if (status == SeatStatus.Held)
            {
                return Colors.Blue.Lighten2;
            }

            return ticketTypeColor != null ? ParseColor(ticketTypeColor) : Colors.Grey.Lighten2;
        }

        private string GetContrastColor(string backgroundColor)
        {
            if (backgroundColor == Colors.White || backgroundColor.Contains("Lighten"))
            {
                return Colors.Black;
            }
            return Colors.White;
        }

        private string ParseColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return Colors.Grey.Lighten2;

            if (hexColor.StartsWith("#"))
            {
                return hexColor;
            }

            return Colors.Grey.Lighten2;
        }

        private class GridCellConfig
        {
            public int r { get; set; }
            public int c { get; set; }
            public string type { get; set; } = string.Empty;
            public string label { get; set; } = string.Empty;
        }
    }
}


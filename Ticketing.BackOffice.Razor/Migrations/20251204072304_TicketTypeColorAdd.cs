using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.BackOffice.Razor.Migrations
{
    /// <inheritdoc />
    public partial class TicketTypeColorAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "TicketTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "TicketTypes");
        }
    }
}

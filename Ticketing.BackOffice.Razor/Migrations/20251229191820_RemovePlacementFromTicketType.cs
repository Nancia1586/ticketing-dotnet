using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.BackOffice.Razor.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlacementFromTicketType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReservedSeating",
                table: "TicketTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReservedSeating",
                table: "TicketTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

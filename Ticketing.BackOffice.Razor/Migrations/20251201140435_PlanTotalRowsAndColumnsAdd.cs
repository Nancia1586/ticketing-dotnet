using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.BackOffice.Razor.Migrations
{
    /// <inheritdoc />
    public partial class PlanTotalRowsAndColumnsAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalColumns",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRows",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalColumns",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TotalRows",
                table: "Events");
        }
    }
}

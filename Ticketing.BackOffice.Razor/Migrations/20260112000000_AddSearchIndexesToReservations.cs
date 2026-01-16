using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.BackOffice.Razor.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexesToReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: SQL Server cannot create indexes on nvarchar(max) columns.
            // We need to alter the columns to have a maximum length first.
            // For search performance, we'll limit these columns to reasonable sizes.
            
            // Alter Reference column to nvarchar(100) to allow indexing
            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Reservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Alter CustomerName column to nvarchar(200) to allow indexing
            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Reservations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Alter Email column to nvarchar(255) to allow indexing
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Reservations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Alter PaymentReference column to nvarchar(100) to allow indexing
            migrationBuilder.AlterColumn<string>(
                name: "PaymentReference",
                table: "Reservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Now create indexes on the altered columns
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Reference",
                table: "Reservations",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerName",
                table: "Reservations",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Email",
                table: "Reservations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PaymentReference",
                table: "Reservations",
                column: "PaymentReference");

            // Index on Status for filtering confirmed reservations
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status",
                table: "Reservations",
                column: "Status");

            // Composite index on EventId and Status for dashboard queries
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_EventId_Status",
                table: "Reservations",
                columns: new[] { "EventId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first
            migrationBuilder.DropIndex(
                name: "IX_Reservations_Reference",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerName",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_Email",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PaymentReference",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_Status",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_EventId_Status",
                table: "Reservations");

            // Revert columns back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentReference",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}


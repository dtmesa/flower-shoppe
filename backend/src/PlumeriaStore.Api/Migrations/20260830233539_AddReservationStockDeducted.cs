using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumeriaStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationStockDeducted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StockDeducted",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockDeducted",
                table: "Reservations");
        }
    }
}

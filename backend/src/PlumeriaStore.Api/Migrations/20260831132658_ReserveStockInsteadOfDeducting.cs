using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumeriaStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReserveStockInsteadOfDeducting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockDeducted",
                table: "Reservations",
                newName: "StockReserved");

            migrationBuilder.RenameColumn(
                name: "QuantityAvailable",
                table: "InventoryItems",
                newName: "QuantityTotal");

            // Under the old model, confirming a request physically decremented the item's stock,
            // so the column already had held units subtracted out. The column now means "units on
            // hand including anything held", so add those units back - otherwise every item with a
            // live confirmed request would appear short by exactly the held amount.
            migrationBuilder.Sql("""
                UPDATE InventoryItems
                SET QuantityTotal = QuantityTotal + COALESCE((
                    SELECT SUM(r.QuantityRequested)
                    FROM Reservations r
                    WHERE r.InventoryItemId = InventoryItems.Id AND r.StockReserved = 1
                ), 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-subtract the held units before renaming back, restoring the old "already
            // deducted" meaning of the column.
            migrationBuilder.Sql("""
                UPDATE InventoryItems
                SET QuantityTotal = MAX(0, QuantityTotal - COALESCE((
                    SELECT SUM(r.QuantityRequested)
                    FROM Reservations r
                    WHERE r.InventoryItemId = InventoryItems.Id AND r.StockReserved = 1
                ), 0));
                """);

            migrationBuilder.RenameColumn(
                name: "StockReserved",
                table: "Reservations",
                newName: "StockDeducted");

            migrationBuilder.RenameColumn(
                name: "QuantityTotal",
                table: "InventoryItems",
                newName: "QuantityAvailable");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumeriaStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryImagePrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "InventoryImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "InventoryImages");
        }
    }
}

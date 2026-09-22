using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreProductDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "sanjcorp",
                table: "ProductCatalogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialCost",
                schema: "sanjcorp",
                table: "ProductCatalogs",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductionMinutes",
                schema: "sanjcorp",
                table: "ProductCatalogs",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "sanjcorp",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "MaterialCost",
                schema: "sanjcorp",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "ProductionMinutes",
                schema: "sanjcorp",
                table: "ProductCatalogs");
        }
    }
}

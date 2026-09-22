using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreProductMaterialAndProfit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FilamentGrams",
                schema: "sanjcorp",
                table: "ProductCatalogs",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaterialType",
                schema: "sanjcorp",
                table: "ProductCatalogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitMultiplier",
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
                name: "FilamentGrams",
                schema: "sanjcorp",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "MaterialType",
                schema: "sanjcorp",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "ProfitMultiplier",
                schema: "sanjcorp",
                table: "ProductCatalogs");
        }
    }
}

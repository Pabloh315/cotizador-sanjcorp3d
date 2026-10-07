using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    [Migration("20261007090000_StoreProductPricingPercentages")]
    public partial class StoreProductPricingPercentages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(name: "MaintenancePercent", schema: "sanjcorp", table: "ProductCatalogs", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 6m);
            migrationBuilder.AddColumn<decimal>(name: "PreparationPercent", schema: "sanjcorp", table: "ProductCatalogs", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 10m);
            migrationBuilder.AddColumn<decimal>(name: "LaborPercent", schema: "sanjcorp", table: "ProductCatalogs", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 20m);
            migrationBuilder.AddColumn<decimal>(name: "WastePercent", schema: "sanjcorp", table: "ProductCatalogs", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 7m);
            migrationBuilder.AddColumn<decimal>(name: "OverheadPercent", schema: "sanjcorp", table: "ProductCatalogs", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 5m);
            migrationBuilder.AddColumn<decimal>(name: "PackagingCost", schema: "sanjcorp", table: "ProductCatalogs", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MaintenancePercent", schema: "sanjcorp", table: "ProductCatalogs");
            migrationBuilder.DropColumn(name: "PreparationPercent", schema: "sanjcorp", table: "ProductCatalogs");
            migrationBuilder.DropColumn(name: "LaborPercent", schema: "sanjcorp", table: "ProductCatalogs");
            migrationBuilder.DropColumn(name: "WastePercent", schema: "sanjcorp", table: "ProductCatalogs");
            migrationBuilder.DropColumn(name: "OverheadPercent", schema: "sanjcorp", table: "ProductCatalogs");
            migrationBuilder.DropColumn(name: "PackagingCost", schema: "sanjcorp", table: "ProductCatalogs");
        }
    }
}


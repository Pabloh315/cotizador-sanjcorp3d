using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[Migration("20261007100000_StoreProductAdditionalMaterials")]
public partial class StoreProductAdditionalMaterials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AdditionalMaterialCost",
            schema: "sanjcorp",
            table: "ProductCatalogs",
            type: "numeric(18,4)",
            precision: 18,
            scale: 4,
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AdditionalMaterialCost",
            schema: "sanjcorp",
            table: "ProductCatalogs");
    }
}

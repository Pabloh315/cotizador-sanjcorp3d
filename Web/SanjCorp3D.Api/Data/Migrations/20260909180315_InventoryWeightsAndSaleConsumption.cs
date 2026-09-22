using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InventoryWeightsAndSaleConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LowStockGrams",
                schema: "sanjcorp",
                table: "Consumables",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1000m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockGrams",
                schema: "sanjcorp",
                table: "Consumables",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Existing values represented 1 kg spool units. Convert them to the
            // new total-grams inventory without discarding current stock.
            migrationBuilder.Sql("UPDATE sanjcorp.\"Consumables\" SET \"StockGrams\" = \"StockQuantity\" * 1000.0;");

            migrationBuilder.CreateTable(
                name: "SaleConsumables",
                schema: "sanjcorp",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleId = table.Column<long>(type: "bigint", nullable: false),
                    ConsumableId = table.Column<long>(type: "bigint", nullable: false),
                    Grams = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleConsumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleConsumables_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalSchema: "sanjcorp",
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleConsumables_Sales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "sanjcorp",
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleConsumables_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleConsumables_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleConsumables",
                schema: "sanjcorp");

            migrationBuilder.DropColumn(
                name: "LowStockGrams",
                schema: "sanjcorp",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "StockGrams",
                schema: "sanjcorp",
                table: "Consumables");
        }
    }
}

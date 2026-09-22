using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SanjCorp3D.Api.Data;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[DbContextAttribute(typeof(AppDbContext))]
[Migration("20260913113000_AddInventoryLotsAndQuotePhone")]
public partial class AddInventoryLotsAndQuotePhone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CustomerPhone", schema: "sanjcorp", table: "Quotes",
            type: "text", nullable: false, defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "PricePerKilogram", schema: "sanjcorp", table: "SaleConsumables",
            type: "numeric(18,4)", nullable: false, defaultValue: 0m);

        migrationBuilder.CreateTable(
            name: "ConsumableStockLots", schema: "sanjcorp",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                ConsumableId = table.Column<long>(type: "bigint", nullable: false),
                OriginalGrams = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                RemainingGrams = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                PricePerKilogram = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsumableStockLots", x => x.Id);
                table.ForeignKey("FK_ConsumableStockLots_Consumables_TenantId_ConsumableId", x => new { x.TenantId, x.ConsumableId }, principalSchema: "sanjcorp", principalTable: "Consumables", principalColumns: new[] { "TenantId", "Id" }, onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_ConsumableStockLots_TenantId_ConsumableId_ReceivedAtUtc", schema: "sanjcorp", table: "ConsumableStockLots", columns: new[] { "TenantId", "ConsumableId", "ReceivedAtUtc" });

        migrationBuilder.CreateTable(
            name: "ChatMessages", schema: "sanjcorp",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                SenderName = table.Column<string>(type: "text", nullable: false),
                Body = table.Column<string>(type: "text", nullable: false),
                PhotoUrl = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatMessages", x => x.Id);
                table.ForeignKey("FK_ChatMessages_Tenants_TenantId", x => x.TenantId, principalSchema: "sanjcorp", principalTable: "Tenants", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_ChatMessages_TenantId_CreatedAtUtc", schema: "sanjcorp", table: "ChatMessages", columns: new[] { "TenantId", "CreatedAtUtc" });

        migrationBuilder.Sql("INSERT INTO sanjcorp.\"ConsumableStockLots\" (\"TenantId\", \"ConsumableId\", \"OriginalGrams\", \"RemainingGrams\", \"PricePerKilogram\", \"ReceivedAtUtc\") SELECT \"TenantId\", \"Id\", \"StockGrams\", \"StockGrams\", \"PricePerUnit\", NOW() FROM sanjcorp.\"Consumables\" WHERE \"StockGrams\" > 0;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ChatMessages", schema: "sanjcorp");
        migrationBuilder.DropTable(name: "ConsumableStockLots", schema: "sanjcorp");
        migrationBuilder.DropColumn(name: "CustomerPhone", schema: "sanjcorp", table: "Quotes");
        migrationBuilder.DropColumn(name: "PricePerKilogram", schema: "sanjcorp", table: "SaleConsumables");
    }
}

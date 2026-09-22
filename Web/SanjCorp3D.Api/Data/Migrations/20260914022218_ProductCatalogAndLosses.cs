using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductCatalogAndLosses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                schema: "sanjcorp",
                table: "Quotes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InventoryLosses",
                schema: "sanjcorp",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumableId = table.Column<long>(type: "bigint", nullable: false),
                    Grams = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLosses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLosses_Consumables_TenantId_ConsumableId",
                        columns: x => new { x.TenantId, x.ConsumableId },
                        principalSchema: "sanjcorp",
                        principalTable: "Consumables",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCatalogs",
                schema: "sanjcorp",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCatalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCatalogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "sanjcorp",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLosses_TenantId_ConsumableId",
                schema: "sanjcorp",
                table: "InventoryLosses",
                columns: new[] { "TenantId", "ConsumableId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLosses_TenantId_CreatedAtUtc",
                schema: "sanjcorp",
                table: "InventoryLosses",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_TenantId_Name",
                schema: "sanjcorp",
                table: "ProductCatalogs",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryLosses",
                schema: "sanjcorp");

            migrationBuilder.DropTable(
                name: "ProductCatalogs",
                schema: "sanjcorp");

            migrationBuilder.DropColumn(
                name: "ProductName",
                schema: "sanjcorp",
                table: "Quotes");
        }
    }
}

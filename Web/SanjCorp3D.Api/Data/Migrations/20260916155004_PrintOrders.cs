using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrintOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PrinterId",
                schema: "sanjcorp",
                table: "Quotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Printers_TenantId_Id",
                schema: "sanjcorp",
                table: "Printers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "PrintOrders",
                schema: "sanjcorp",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<long>(type: "bigint", nullable: false),
                    PrinterId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pendiente"),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedFinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CoolingUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintOrders_Printers_TenantId_PrinterId",
                        columns: x => new { x.TenantId, x.PrinterId },
                        principalSchema: "sanjcorp",
                        principalTable: "Printers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintOrders_Quotes_TenantId_QuoteId",
                        columns: x => new { x.TenantId, x.QuoteId },
                        principalSchema: "sanjcorp",
                        principalTable: "Quotes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrintOrders_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "sanjcorp",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintOrders_TenantId_PrinterId_CreatedAtUtc",
                schema: "sanjcorp",
                table: "PrintOrders",
                columns: new[] { "TenantId", "PrinterId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintOrders_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "PrintOrders",
                columns: new[] { "TenantId", "QuoteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintOrders",
                schema: "sanjcorp");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Printers_TenantId_Id",
                schema: "sanjcorp",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PrinterId",
                schema: "sanjcorp",
                table: "Quotes");
        }
    }
}

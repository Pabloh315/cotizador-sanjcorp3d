using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultiplePrintOrdersPerQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrintOrders_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "PrintOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PrintOrders_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "PrintOrders",
                columns: new[] { "TenantId", "QuoteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrintOrders_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "PrintOrders");

            migrationBuilder.CreateIndex(
                name: "IX_PrintOrders_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "PrintOrders",
                columns: new[] { "TenantId", "QuoteId" },
                unique: true);
        }
    }
}

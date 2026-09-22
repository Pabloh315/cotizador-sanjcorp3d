using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPrintOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE sanjcorp."Quotes" AS q
                SET "PrinterId" = p."Id"
                FROM sanjcorp."Printers" AS p
                WHERE q."TenantId" = p."TenantId"
                  AND q."PrinterName" = p."Name"
                  AND q."PrinterId" IS NULL;

                INSERT INTO sanjcorp."PrintOrders" ("TenantId", "QuoteId", "PrinterId", "Status", "CreatedAtUtc")
                SELECT q."TenantId", q."Id", q."PrinterId", 'Pendiente', s."SoldAtUtc"
                FROM sanjcorp."Quotes" AS q
                INNER JOIN sanjcorp."Sales" AS s ON s."TenantId" = q."TenantId" AND s."QuoteId" = q."Id"
                WHERE q."PrinterId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM sanjcorp."PrintOrders" AS po
                      WHERE po."TenantId" = q."TenantId" AND po."QuoteId" = q."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

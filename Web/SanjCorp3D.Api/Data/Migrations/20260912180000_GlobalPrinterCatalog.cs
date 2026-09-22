using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SanjCorp3D.Api.Data;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[DbContextAttribute(typeof(AppDbContext))]
[Migration("20260912180000_GlobalPrinterCatalog")]
public partial class GlobalPrinterCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Some deployments may already contain this column because the
        // repair SQL was applied manually. Keep this migration idempotent.
        migrationBuilder.Sql("""
            ALTER TABLE "sanjcorp"."Printers"
                ADD COLUMN IF NOT EXISTS "CatalogId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
            """);

        // Give old rows a stable global identity based on their normalized name.
        migrationBuilder.Sql("""
            UPDATE "sanjcorp"."Printers"
            SET "CatalogId" = md5(lower(trim("Name")))::uuid
            WHERE "CatalogId" = '00000000-0000-0000-0000-000000000000';
            """);

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Printers_CatalogId\" ON \"sanjcorp\".\"Printers\" (\"CatalogId\");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Printers_CatalogId",
            schema: "sanjcorp",
            table: "Printers");

        migrationBuilder.DropColumn(
            name: "CatalogId",
            schema: "sanjcorp",
            table: "Printers");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SanjCorp3D.Api.Data;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[DbContextAttribute(typeof(AppDbContext))]
[Migration("20260912190000_RepairGlobalPrinterCatalog")]
public partial class RepairGlobalPrinterCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This migration is intentionally idempotent. It repairs deployments
        // where the previous migration was recorded but its schema change was
        // not applied, without touching quotes, sales, users, or tenants.
        migrationBuilder.Sql("""
            ALTER TABLE "sanjcorp"."Printers"
                ADD COLUMN IF NOT EXISTS "CatalogId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

            UPDATE "sanjcorp"."Printers"
            SET "CatalogId" = md5(lower(trim("Name")))::uuid
            WHERE "CatalogId" = '00000000-0000-0000-0000-000000000000';

            CREATE INDEX IF NOT EXISTS "IX_Printers_CatalogId"
                ON "sanjcorp"."Printers" ("CatalogId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"sanjcorp\".\"IX_Printers_CatalogId\";");
    }
}

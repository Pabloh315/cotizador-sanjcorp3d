using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SanjCorp3D.Api.Data;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260913110000_AddMakerOwner")]
public partial class AddMakerOwner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsMakerOwner",
            schema: "sanjcorp",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Preserve existing Maker spaces by assigning ownership to their
        // earliest Maker user. New spaces set this flag during creation.
        migrationBuilder.Sql("""
            WITH ranked AS (
                SELECT u."Id",
                       ROW_NUMBER() OVER (PARTITION BY u."TenantId" ORDER BY u."CreatedAtUtc", u."Id") AS rn
                FROM sanjcorp."AspNetUsers" u
                INNER JOIN sanjcorp."AspNetUserRoles" ur ON ur."UserId" = u."Id"
                INNER JOIN sanjcorp."AspNetRoles" r ON r."Id" = ur."RoleId"
                INNER JOIN sanjcorp."Tenants" t ON t."Id" = u."TenantId"
                WHERE r."Name" = 'Maker' AND t."Kind" = 'maker'
            )
            UPDATE sanjcorp."AspNetUsers" u
            SET "IsMakerOwner" = TRUE
            FROM ranked
            WHERE u."Id" = ranked."Id" AND ranked.rn = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsMakerOwner",
            schema: "sanjcorp",
            table: "AspNetUsers");
    }
}

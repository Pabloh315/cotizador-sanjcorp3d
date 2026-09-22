using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrinterColorsAndConsumableMaterialTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColorCount",
                schema: "sanjcorp",
                table: "Printers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ConsumableMaterialTypes",
                schema: "sanjcorp",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableMaterialTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumableMaterialTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "sanjcorp",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableMaterialTypes_TenantId_Name",
                schema: "sanjcorp",
                table: "ConsumableMaterialTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumableMaterialTypes",
                schema: "sanjcorp");

            migrationBuilder.DropColumn(
                name: "ColorCount",
                schema: "sanjcorp",
                table: "Printers");
        }
    }
}

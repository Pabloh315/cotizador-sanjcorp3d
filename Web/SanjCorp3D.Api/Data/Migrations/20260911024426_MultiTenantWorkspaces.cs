using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiTenantWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_OrderCode",
                schema: "sanjcorp",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Printers_Name",
                schema: "sanjcorp",
                table: "Printers");

            migrationBuilder.DropIndex(
                name: "IX_Materials_Name",
                schema: "sanjcorp",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_Name_Material_Color",
                schema: "sanjcorp",
                table: "Consumables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSettings",
                schema: "sanjcorp",
                table: "BusinessSettings");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Sales",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Quotes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Printers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Materials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Consumables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "BusinessSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsSupremeAdmin",
                schema: "sanjcorp",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSettings",
                schema: "sanjcorp",
                table: "BusinessSettings",
                columns: new[] { "TenantId", "Key" });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "sanjcorp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO \"sanjcorp\".\"Tenants\" (\"Id\",\"Name\",\"Slug\",\"Kind\",\"Active\",\"CreatedAtUtc\") VALUES ('00000000-0000-0000-0000-000000000001','SanjCorp Technology','sanjcorp-technology','technology',TRUE, CURRENT_TIMESTAMP);");
            migrationBuilder.Sql("UPDATE \"sanjcorp\".\"Sales\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001'; UPDATE \"sanjcorp\".\"Quotes\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001'; UPDATE \"sanjcorp\".\"Printers\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001'; UPDATE \"sanjcorp\".\"Materials\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001'; UPDATE \"sanjcorp\".\"Consumables\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001'; UPDATE \"sanjcorp\".\"BusinessSettings\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001'; UPDATE \"sanjcorp\".\"AspNetUsers\" SET \"TenantId\" = '00000000-0000-0000-0000-000000000001' WHERE \"TenantId\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "Sales",
                columns: new[] { "TenantId", "QuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_TenantId_OrderCode",
                schema: "sanjcorp",
                table: "Quotes",
                columns: new[] { "TenantId", "OrderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Printers_TenantId_Name",
                schema: "sanjcorp",
                table: "Printers",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_TenantId_Name",
                schema: "sanjcorp",
                table: "Materials",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_TenantId_Name_Material_Color",
                schema: "sanjcorp",
                table: "Consumables",
                columns: new[] { "TenantId", "Name", "Material", "Color" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                schema: "sanjcorp",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessSettings_Tenants_TenantId",
                schema: "sanjcorp",
                table: "BusinessSettings",
                column: "TenantId",
                principalSchema: "sanjcorp",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Consumables_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Consumables",
                column: "TenantId",
                principalSchema: "sanjcorp",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Materials",
                column: "TenantId",
                principalSchema: "sanjcorp",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Printers_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Printers",
                column: "TenantId",
                principalSchema: "sanjcorp",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Quotes",
                column: "TenantId",
                principalSchema: "sanjcorp",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Sales",
                column: "TenantId",
                principalSchema: "sanjcorp",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessSettings_Tenants_TenantId",
                schema: "sanjcorp",
                table: "BusinessSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumables_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Consumables");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_Printers_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Printers");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Tenants_TenantId",
                schema: "sanjcorp",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "sanjcorp");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_TenantId_OrderCode",
                schema: "sanjcorp",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Printers_TenantId_Name",
                schema: "sanjcorp",
                table: "Printers");

            migrationBuilder.DropIndex(
                name: "IX_Materials_TenantId_Name",
                schema: "sanjcorp",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_TenantId_Name_Material_Color",
                schema: "sanjcorp",
                table: "Consumables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessSettings",
                schema: "sanjcorp",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "IsSupremeAdmin",
                schema: "sanjcorp",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "AspNetUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessSettings",
                schema: "sanjcorp",
                table: "BusinessSettings",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_OrderCode",
                schema: "sanjcorp",
                table: "Quotes",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Printers_Name",
                schema: "sanjcorp",
                table: "Printers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Name",
                schema: "sanjcorp",
                table: "Materials",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_Name_Material_Color",
                schema: "sanjcorp",
                table: "Consumables",
                columns: new[] { "Name", "Material", "Color" },
                unique: true);
        }
    }
}

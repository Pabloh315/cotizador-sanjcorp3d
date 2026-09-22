using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiTenantChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteConsumables_Quotes_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_QuoteMaterials_Quotes_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleConsumables_Consumables_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleConsumables_Sales_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropIndex(
                name: "IX_QuoteMaterials_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials");

            migrationBuilder.DropIndex(
                name: "IX_QuoteConsumables_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));


            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "QuoteMaterials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "sanjcorp",
                table: "QuoteConsumables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE \"sanjcorp\".\"QuoteConsumables\" c SET \"TenantId\" = q.\"TenantId\" FROM \"sanjcorp\".\"Quotes\" q WHERE q.\"Id\" = c.\"QuoteId\"; UPDATE \"sanjcorp\".\"QuoteMaterials\" c SET \"TenantId\" = q.\"TenantId\" FROM \"sanjcorp\".\"Quotes\" q WHERE q.\"Id\" = c.\"QuoteId\"; UPDATE \"sanjcorp\".\"SaleConsumables\" c SET \"TenantId\" = s.\"TenantId\" FROM \"sanjcorp\".\"Sales\" s WHERE s.\"Id\" = c.\"SaleId\";");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Sales_TenantId_Id",
                schema: "sanjcorp",
                table: "Sales",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Quotes_TenantId_Id",
                schema: "sanjcorp",
                table: "Quotes",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Consumables_TenantId_Id",
                schema: "sanjcorp",
                table: "Consumables",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleConsumables_TenantId_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                columns: new[] { "TenantId", "ConsumableId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleConsumables_TenantId_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                columns: new[] { "TenantId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteMaterials_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials",
                columns: new[] { "TenantId", "QuoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteConsumables_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables",
                columns: new[] { "TenantId", "QuoteId" });

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteConsumables_Quotes_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables",
                columns: new[] { "TenantId", "QuoteId" },
                principalSchema: "sanjcorp",
                principalTable: "Quotes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteMaterials_Quotes_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials",
                columns: new[] { "TenantId", "QuoteId" },
                principalSchema: "sanjcorp",
                principalTable: "Quotes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleConsumables_Consumables_TenantId_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                columns: new[] { "TenantId", "ConsumableId" },
                principalSchema: "sanjcorp",
                principalTable: "Consumables",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleConsumables_Sales_TenantId_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                columns: new[] { "TenantId", "SaleId" },
                principalSchema: "sanjcorp",
                principalTable: "Sales",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteConsumables_Quotes_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_QuoteMaterials_Quotes_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleConsumables_Consumables_TenantId_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleConsumables_Sales_TenantId_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Sales_TenantId_Id",
                schema: "sanjcorp",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleConsumables_TenantId_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropIndex(
                name: "IX_SaleConsumables_TenantId_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Quotes_TenantId_Id",
                schema: "sanjcorp",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteMaterials_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials");

            migrationBuilder.DropIndex(
                name: "IX_QuoteConsumables_TenantId_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Consumables_TenantId_Id",
                schema: "sanjcorp",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "SaleConsumables");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "QuoteMaterials");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "sanjcorp",
                table: "QuoteConsumables");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteMaterials_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteConsumables_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables",
                column: "QuoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteConsumables_Quotes_QuoteId",
                schema: "sanjcorp",
                table: "QuoteConsumables",
                column: "QuoteId",
                principalSchema: "sanjcorp",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteMaterials_Quotes_QuoteId",
                schema: "sanjcorp",
                table: "QuoteMaterials",
                column: "QuoteId",
                principalSchema: "sanjcorp",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleConsumables_Consumables_ConsumableId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                column: "ConsumableId",
                principalSchema: "sanjcorp",
                principalTable: "Consumables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleConsumables_Sales_SaleId",
                schema: "sanjcorp",
                table: "SaleConsumables",
                column: "SaleId",
                principalSchema: "sanjcorp",
                principalTable: "Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

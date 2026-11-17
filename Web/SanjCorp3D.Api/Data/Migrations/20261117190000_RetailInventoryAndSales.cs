using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261117190000_RetailInventoryAndSales")]
public sealed class RetailInventoryAndSales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "RetailProducts", schema: "sanjcorp", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            TenantId = table.Column<Guid>(type: "uuid", nullable: false), Category = table.Column<string>(type: "text", nullable: false),
            Brand = table.Column<string>(type: "text", nullable: false), Name = table.Column<string>(type: "text", nullable: false), Sku = table.Column<string>(type: "text", nullable: false),
            MaterialType = table.Column<string>(type: "text", nullable: false), ColorName = table.Column<string>(type: "text", nullable: false), ColorHex = table.Column<string>(type: "text", nullable: false),
            Specifications = table.Column<string>(type: "text", nullable: false), UnitWeightGrams = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
            UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false), ProfitPercent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
            StockQuantity = table.Column<int>(type: "integer", nullable: false), Active = table.Column<bool>(type: "boolean", nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_RetailProducts", x => x.Id); table.UniqueConstraint("AK_RetailProducts_TenantId_Id", x => new { x.TenantId, x.Id }); table.ForeignKey("FK_RetailProducts_Tenants_TenantId", x => x.TenantId, "sanjcorp", "Tenants", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "RetailAcquisitions", schema: "sanjcorp", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), TenantId = table.Column<Guid>(type: "uuid", nullable: false),
            ProductId = table.Column<long>(type: "bigint", nullable: false), Quantity = table.Column<int>(type: "integer", nullable: false), UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
            Supplier = table.Column<string>(type: "text", nullable: false), AcquiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_RetailAcquisitions", x => x.Id); table.ForeignKey("FK_RetailAcquisitions_RetailProducts_TenantId_ProductId", x => new { x.TenantId, x.ProductId }, "sanjcorp", "RetailProducts", new[] { "TenantId", "Id" }, onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "RetailSales", schema: "sanjcorp", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), TenantId = table.Column<Guid>(type: "uuid", nullable: false), ProductId = table.Column<long>(type: "bigint", nullable: false),
            Category = table.Column<string>(type: "text", nullable: false), ProductName = table.Column<string>(type: "text", nullable: false), Customer = table.Column<string>(type: "text", nullable: false), Quantity = table.Column<int>(type: "integer", nullable: false),
            UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false), ProfitPercent = table.Column<decimal>(type: "numeric(18,4)", nullable: false), UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
            TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false), TotalPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false), Profit = table.Column<decimal>(type: "numeric(18,4)", nullable: false), SoldAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_RetailSales", x => x.Id); table.ForeignKey("FK_RetailSales_RetailProducts_TenantId_ProductId", x => new { x.TenantId, x.ProductId }, "sanjcorp", "RetailProducts", new[] { "TenantId", "Id" }, onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateIndex("IX_RetailProducts_TenantId_Category_Brand_Name_MaterialType_ColorName", "RetailProducts", new[] { "TenantId", "Category", "Brand", "Name", "MaterialType", "ColorName" }, schema: "sanjcorp");
        migrationBuilder.CreateIndex("IX_RetailAcquisitions_TenantId_AcquiredAtUtc", "RetailAcquisitions", new[] { "TenantId", "AcquiredAtUtc" }, schema: "sanjcorp");
        migrationBuilder.CreateIndex("IX_RetailAcquisitions_TenantId_ProductId", "RetailAcquisitions", new[] { "TenantId", "ProductId" }, schema: "sanjcorp");
        migrationBuilder.CreateIndex("IX_RetailSales_TenantId_SoldAtUtc", "RetailSales", new[] { "TenantId", "SoldAtUtc" }, schema: "sanjcorp");
        migrationBuilder.CreateIndex("IX_RetailSales_TenantId_ProductId", "RetailSales", new[] { "TenantId", "ProductId" }, schema: "sanjcorp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("RetailAcquisitions", "sanjcorp"); migrationBuilder.DropTable("RetailSales", "sanjcorp"); migrationBuilder.DropTable("RetailProducts", "sanjcorp");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SanjCorp3D.Api.Data;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations;

[DbContextAttribute(typeof(AppDbContext))]
[Migration("20260912120000_AddSaleCreatedByUser")]
public partial class AddSaleCreatedByUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CreatedByUserId",
            schema: "sanjcorp",
            table: "Sales",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedByUserId",
            schema: "sanjcorp",
            table: "Sales");
    }
}

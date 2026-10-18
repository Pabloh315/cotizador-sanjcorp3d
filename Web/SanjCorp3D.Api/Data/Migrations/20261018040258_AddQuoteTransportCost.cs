using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    public partial class AddQuoteTransportCost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TransportCost",
                schema: "sanjcorp",
                table: "Quotes",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransportCost",
                schema: "sanjcorp",
                table: "Quotes");
        }
    }
}

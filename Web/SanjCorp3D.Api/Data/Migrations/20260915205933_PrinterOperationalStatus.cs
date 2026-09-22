using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrinterOperationalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "sanjcorp",
                table: "Printers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Disponible");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "sanjcorp",
                table: "Printers");
        }
    }
}

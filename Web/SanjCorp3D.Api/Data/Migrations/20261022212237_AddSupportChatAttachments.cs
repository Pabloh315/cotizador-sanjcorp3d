using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanjCorp3D.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportChatAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentContentType",
                schema: "sanjcorp",
                table: "ChatMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                schema: "sanjcorp",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentContentType",
                schema: "sanjcorp",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                schema: "sanjcorp",
                table: "ChatMessages");
        }
    }
}

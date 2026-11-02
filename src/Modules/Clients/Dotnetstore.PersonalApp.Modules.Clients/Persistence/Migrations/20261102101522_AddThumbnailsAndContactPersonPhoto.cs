using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Clients.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailsAndContactPersonPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                table: "ContactPersons",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoThumbnail",
                table: "ContactPersons",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoThumbnail",
                table: "Clients",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Photo",
                table: "ContactPersons");

            migrationBuilder.DropColumn(
                name: "PhotoThumbnail",
                table: "ContactPersons");

            migrationBuilder.DropColumn(
                name: "PhotoThumbnail",
                table: "Clients");
        }
    }
}

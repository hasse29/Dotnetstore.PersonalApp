using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Inventory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceLine",
                table: "StockMovements",
                type: "TEXT",
                maxLength: 36,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceLine",
                table: "StockMovements");
        }
    }
}

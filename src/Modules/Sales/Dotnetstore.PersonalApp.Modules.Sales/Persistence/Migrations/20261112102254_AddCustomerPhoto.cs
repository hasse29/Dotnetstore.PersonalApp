using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCustomerPhoto : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "Photo",
            table: "Customers",
            type: "BLOB",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "PhotoThumbnail",
            table: "Customers",
            type: "BLOB",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Photo",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "PhotoThumbnail",
            table: "Customers");
    }
}

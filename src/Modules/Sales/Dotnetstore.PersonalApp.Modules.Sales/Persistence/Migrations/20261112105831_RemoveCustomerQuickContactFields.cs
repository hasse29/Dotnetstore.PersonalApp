using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence.Migrations;

/// <inheritdoc />
public partial class RemoveCustomerQuickContactFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "City",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "Email",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "Phone",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "PostalCode",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "Street",
            table: "Customers");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "City",
            table: "Customers",
            type: "TEXT",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "Customers",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Phone",
            table: "Customers",
            type: "TEXT",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PostalCode",
            table: "Customers",
            type: "TEXT",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Street",
            table: "Customers",
            type: "TEXT",
            maxLength: 200,
            nullable: true);
    }
}

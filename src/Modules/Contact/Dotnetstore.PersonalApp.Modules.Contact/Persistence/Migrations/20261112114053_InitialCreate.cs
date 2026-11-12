using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Dotnetstore.PersonalApp.Modules.Contact.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261112114053_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AddressTypes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AddressTypes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EmailAddressTypes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailAddressTypes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PhoneNumberTypes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PhoneNumberTypes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Addresses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ObjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                AddressTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                CareOf = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Street1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Street2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Addresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Addresses_AddressTypes_AddressTypeId",
                    column: x => x.AddressTypeId,
                    principalTable: "AddressTypes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EmailAddresses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ObjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmailAddressTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false, collation: "NOCASE")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailAddresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmailAddresses_EmailAddressTypes_EmailAddressTypeId",
                    column: x => x.EmailAddressTypeId,
                    principalTable: "EmailAddressTypes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PhoneNumbers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ObjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                PhoneNumberTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                Number = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                NormalizedNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PhoneNumbers", x => x.Id);
                table.ForeignKey(
                    name: "FK_PhoneNumbers_PhoneNumberTypes_PhoneNumberTypeId",
                    column: x => x.PhoneNumberTypeId,
                    principalTable: "PhoneNumberTypes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "AddressTypes",
            columns: new[] { "Id", "CreatedAt", "Description", "Name" },
            values: new object[,]
            {
                { new Guid("0197c0de-0000-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Where invoices are sent.", "Billing" },
                { new Guid("0197c0de-0000-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Where goods are delivered.", "Delivery" },
                { new Guid("0197c0de-0000-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Where to meet in person.", "Visiting" },
                { new Guid("0197c0de-0000-7000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A private home address.", "Home" }
            });

        migrationBuilder.InsertData(
            table: "EmailAddressTypes",
            columns: new[] { "Id", "CreatedAt", "Description", "Name" },
            values: new object[,]
            {
                { new Guid("0197c0de-0001-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A work email address.", "Work" },
                { new Guid("0197c0de-0001-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A private email address.", "Private" },
                { new Guid("0197c0de-0001-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Where invoices are sent.", "Billing" }
            });

        migrationBuilder.InsertData(
            table: "PhoneNumberTypes",
            columns: new[] { "Id", "CreatedAt", "Description", "Name" },
            values: new object[,]
            {
                { new Guid("0197c0de-0002-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A mobile phone number.", "Mobile" },
                { new Guid("0197c0de-0002-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A work phone number.", "Work" },
                { new Guid("0197c0de-0002-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "A private landline.", "Home" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Addresses_AddressTypeId",
            table: "Addresses",
            column: "AddressTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_Addresses_ObjectId",
            table: "Addresses",
            column: "ObjectId");

        migrationBuilder.CreateIndex(
            name: "IX_AddressTypes_Name",
            table: "AddressTypes",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmailAddresses_EmailAddressTypeId",
            table: "EmailAddresses",
            column: "EmailAddressTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_EmailAddresses_ObjectId_Email",
            table: "EmailAddresses",
            columns: new[] { "ObjectId", "Email" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmailAddressTypes_Name",
            table: "EmailAddressTypes",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PhoneNumbers_ObjectId_NormalizedNumber",
            table: "PhoneNumbers",
            columns: new[] { "ObjectId", "NormalizedNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PhoneNumbers_PhoneNumberTypeId",
            table: "PhoneNumbers",
            column: "PhoneNumberTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_PhoneNumberTypes_Name",
            table: "PhoneNumberTypes",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Addresses");

        migrationBuilder.DropTable(
            name: "EmailAddresses");

        migrationBuilder.DropTable(
            name: "PhoneNumbers");

        migrationBuilder.DropTable(
            name: "AddressTypes");

        migrationBuilder.DropTable(
            name: "EmailAddressTypes");

        migrationBuilder.DropTable(
            name: "PhoneNumberTypes");
    }
}

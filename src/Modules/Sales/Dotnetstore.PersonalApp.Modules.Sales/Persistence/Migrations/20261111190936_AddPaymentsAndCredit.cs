using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnetstore.PersonalApp.Modules.Sales.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoicePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Excess = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicePayments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditEntries_CustomerId",
                table: "CreditEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditEntries_PaymentId",
                table: "CreditEntries",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePayments_InvoiceId",
                table: "InvoicePayments",
                column: "InvoiceId");

            // Invoices marked as paid before payments existed get one payment for the full amount, on the day they were marked paid.
            migrationBuilder.Sql(
                @"INSERT INTO InvoicePayments (Id, InvoiceId, Kind, Date, Amount, Excess, Note, CreatedAt)
                  SELECT upper(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)), 2) || '-A' || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6))),
                         i.Id, 'Received', COALESCE(i.PaidDate, i.Date),
                         printf('%.2f', (SELECT SUM(n + ROUND(n * VatRate / 100.0, 2))
                                         FROM (SELECT VatRate, ROUND(Quantity * UnitPrice * (1 - DiscountPercent / 100.0), 2) AS n
                                               FROM InvoiceLines WHERE InvoiceId = i.Id))),
                         '0.00', NULL, i.CreatedAt
                  FROM Invoices i WHERE i.Status = 'Paid';");

            migrationBuilder.DropColumn(
                name: "PaidDate",
                table: "Invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditEntries");

            migrationBuilder.DropTable(
                name: "InvoicePayments");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PaidDate",
                table: "Invoices",
                type: "TEXT",
                nullable: true);
        }
    }
}

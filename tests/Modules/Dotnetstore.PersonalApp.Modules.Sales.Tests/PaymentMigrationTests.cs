using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class PaymentMigrationTests
{
    [Fact]
    public async Task AnInvoiceMarkedPaidBeforePaymentsGetsOneFullPayment()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options;

        await using (var before = new SalesDbContext(options))
        {
            await before.GetService<IMigrator>().MigrateAsync("AddInvoicesAndPickAllocations", TestContext.Current.CancellationToken);

            // Two lines: 4 x 100 less 10 % = 360 + 25 % VAT = 90, and 1 x 50 + 6 % VAT = 3; 503.00 in all.
            await before.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Customers (Id, Name, IsActive, CreatedAt) VALUES ('C0000000-0000-0000-0000-000000000001', 'Acme', 1, '2026-11-01 00:00:00+00:00');
                INSERT INTO Offers (Id, Number, CustomerId, CustomerName, Date, ValidUntil, Status, CreatedAt)
                  VALUES ('A0000000-0000-0000-0000-000000000001', 'O-1', 'C0000000-0000-0000-0000-000000000001', 'Acme', '2026-11-01', '2026-12-01', 'Accepted', '2026-11-01 00:00:00+00:00');
                INSERT INTO Orders (Id, Number, OfferId, OfferNumber, CustomerId, CustomerName, Date, Status, CreatedAt)
                  VALUES ('B0000000-0000-0000-0000-000000000001', 'ORD-1', 'A0000000-0000-0000-0000-000000000001', 'O-1', 'C0000000-0000-0000-0000-000000000001', 'Acme', '2026-11-01', 'Invoiced', '2026-11-01 00:00:00+00:00');
                INSERT INTO Invoices (Id, Number, OrderId, OrderNumber, CustomerId, CustomerName, Date, DueDate, Status, PaidDate, CreatedAt)
                  VALUES ('D0000000-0000-0000-0000-000000000001', 'INV-1', 'B0000000-0000-0000-0000-000000000001', 'ORD-1', 'C0000000-0000-0000-0000-000000000001', 'Acme', '2026-11-02', '2026-12-02', 'Paid', '2026-11-20', '2026-11-02 00:00:00+00:00');
                INSERT INTO InvoiceLines (Id, InvoiceId, Position, Description, Quantity, Unit, UnitPrice, VatRate, DiscountPercent)
                  VALUES ('E0000000-0000-0000-0000-000000000001', 'D0000000-0000-0000-0000-000000000001', 1, 'Work', '4', 'Hour', '100', '25', '10'),
                         ('E0000000-0000-0000-0000-000000000002', 'D0000000-0000-0000-0000-000000000001', 2, 'Part', '1', 'Piece', '50', '6', '0');
                """,
                TestContext.Current.CancellationToken);

            await before.Database.MigrateAsync(TestContext.Current.CancellationToken);
        }

        await using var after = new SalesDbContext(options);
        var payment = await after.Payments.SingleAsync(TestContext.Current.CancellationToken);
        payment.Kind.ShouldBe(PaymentKind.Received);
        payment.Amount.ShouldBe(503m);
        payment.Excess.ShouldBe(0m);
        payment.Date.ShouldBe(new DateOnly(2026, 11, 20));
    }
}

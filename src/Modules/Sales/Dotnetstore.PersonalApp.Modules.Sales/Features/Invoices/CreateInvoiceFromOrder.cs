using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>
/// Invoices an order that is ready: everything on its pick list is picked. The lines are copied, the invoice is due after the
/// given number of days, and the order becomes Invoiced.
/// </summary>
internal static class CreateInvoiceFromOrder
{
    public const int DefaultPaymentDays = 30;
    public const int MaxPaymentDays = 365;

    public sealed record Command(Guid OrderId, int PaymentDays = DefaultPaymentDays);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<InvoiceDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<InvoiceDetailDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.PaymentDays is < 0 or > MaxPaymentDays)
            {
                return Error.Validation("invoice_payment_days_invalid", "The payment terms must be between 0 and 365 days.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken).ConfigureAwait(false);
            if (order is null)
            {
                return Error.NotFound("order_not_found", "Order not found.");
            }

            if (order.Status != OrderStatus.ReadyToInvoice)
            {
                return Error.Conflict("invoice_order_not_ready", "Only an order that is ready to invoice can be invoiced.");
            }

            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            var invoice = new Invoice
            {
                Id = Guid.CreateVersion7(),
                Number = await NextNumberAsync(db, today.Year, cancellationToken).ConfigureAwait(false),
                OrderId = order.Id,
                OrderNumber = order.Number,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                CustomerAddress = order.CustomerAddress,
                Reference = order.Reference,
                Notes = order.Notes,
                Date = today,
                DueDate = today.AddDays(command.PaymentDays),
                Status = InvoiceStatus.Unpaid,
                CreatedAt = clock.GetUtcNow(),
                Lines = [.. order.Lines.OrderBy(l => l.Position).Select(l => new InvoiceLine
                {
                    Id = Guid.CreateVersion7(),
                    Position = l.Position,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    VatRate = l.VatRate,
                    DiscountPercent = l.DiscountPercent,
                })],
            };

            order.Status = OrderStatus.Invoiced;
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return InvoiceDetailDto.From(invoice, today);
        }

        // One more than the highest number of the year, so a number is never reused, not even after a cancellation.
        private static async Task<string> NextNumberAsync(SalesDbContext db, int year, CancellationToken cancellationToken)
        {
            var prefix = string.Create(CultureInfo.InvariantCulture, $"INV-{year}-");
            var used = await db.Invoices
                .Where(i => i.Number.StartsWith(prefix))
                .Select(i => i.Number)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var highest = used
                .Select(n => int.TryParse(n.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)
                .DefaultIfEmpty(0)
                .Max();

            return string.Create(CultureInfo.InvariantCulture, $"{prefix}{highest + 1:0000}");
        }
    }
}

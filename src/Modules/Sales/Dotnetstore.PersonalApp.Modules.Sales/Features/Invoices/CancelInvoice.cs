using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>
/// Cancels an invoice that has no payments, for example one that was made by mistake. It keeps its number, and the order is ready
/// to invoice again. An invoice with payments cannot be cancelled until they are removed.
/// </summary>
internal static class CancelInvoice
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<InvoiceDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<InvoiceDetailDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoice = await InvoicePayments.LoadAsync(db, command.Id, cancellationToken).ConfigureAwait(false);
            if (invoice is null)
            {
                return Error.NotFound("invoice_not_found", "Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return Error.Conflict("invoice_already_cancelled", "The invoice is already cancelled.");
            }

            if (invoice.Payments.Count > 0)
            {
                return Error.Conflict("invoice_has_payments", "The invoice has payments. Remove them before cancelling it.");
            }

            invoice.Status = InvoiceStatus.Cancelled;

            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == invoice.OrderId, cancellationToken).ConfigureAwait(false);
            if (order is { Status: OrderStatus.Invoiced })
            {
                order.Status = OrderStatus.ReadyToInvoice;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await InvoicePayments.DetailAsync(db, invoice, DateOnly.FromDateTime(clock.GetLocalNow().DateTime), cancellationToken).ConfigureAwait(false);
        }
    }
}

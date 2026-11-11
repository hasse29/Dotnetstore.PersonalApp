using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>
/// Cancels an unpaid invoice, for example one that was made by mistake. It keeps its number, and the order is ready to invoice
/// again. A paid invoice cannot be cancelled.
/// </summary>
internal static class CancelInvoice
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<InvoiceDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (invoice is null)
            {
                return Error.NotFound("invoice_not_found", "Invoice not found.");
            }

            if (invoice.Status != InvoiceStatus.Unpaid)
            {
                return Error.Conflict("invoice_not_unpaid", "Only an unpaid invoice can be cancelled.");
            }

            invoice.Status = InvoiceStatus.Cancelled;

            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == invoice.OrderId, cancellationToken).ConfigureAwait(false);
            if (order is { Status: OrderStatus.Invoiced })
            {
                order.Status = OrderStatus.ReadyToInvoice;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return InvoiceDetailDto.From(invoice, DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        }
    }
}

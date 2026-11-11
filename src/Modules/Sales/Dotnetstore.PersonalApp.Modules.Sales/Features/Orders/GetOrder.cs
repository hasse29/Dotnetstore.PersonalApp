using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

internal static class GetOrder
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<OrderDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var order = await db.Orders.AsNoTracking()
                .Include(o => o.Lines)
                .Include(o => o.PickList!).ThenInclude(p => p.Lines)
                .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            if (order is null)
            {
                return Error.NotFound("order_not_found", "Order not found.");
            }

            // The invoice of the order, if there is a live one.
            var invoiceNumber = await db.Invoices.AsNoTracking()
                .Where(i => i.OrderId == order.Id && i.Status != InvoiceStatus.Cancelled)
                .Select(i => i.Number)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return OrderDetailDto.From(order) with { InvoiceNumber = invoiceNumber };
        }
    }
}

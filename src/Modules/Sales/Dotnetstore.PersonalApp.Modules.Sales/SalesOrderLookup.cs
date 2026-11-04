using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Orders;
using Dotnetstore.PersonalApp.Shared.Sales;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales;

internal sealed class SalesOrderLookup(IDbContextFactory<SalesDbContext> dbFactory) : ISalesOrderLookup
{
    private const int MaxOrders = 200;

    public async Task<IReadOnlyList<OrderInfo>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.SalesOrders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .Take(MaxOrders)
            .Select(x => new OrderInfo(x.Id, x.Number, x.Customer.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrderLineInfo>> ListLinesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.SalesOrderDetails.AsNoTracking()
            .Where(x => x.SalesOrderId == orderId)
            .Select(x => new OrderLineInfo(x.Id, x.ProductId, x.OrderQty))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

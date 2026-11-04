using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Orders;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing;

internal sealed class PurchaseOrderLookup(IDbContextFactory<PurchasingDbContext> dbFactory) : IPurchaseOrderLookup
{
    private const int MaxOrders = 200;

    public async Task<IReadOnlyList<OrderInfo>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.PurchaseOrders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .Take(MaxOrders)
            .Select(x => new OrderInfo(x.Id, x.Number, x.Vendor.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrderLineInfo>> ListLinesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.PurchaseOrderDetails.AsNoTracking()
            .Where(x => x.PurchaseOrderId == orderId)
            .Select(x => new OrderLineInfo(x.Id, x.ProductId, x.OrderQty))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

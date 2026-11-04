using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Consumers;

/// <summary>Records what the warehouse has shipped on a line, and marks the order shipped once every line has gone out.</summary>
internal sealed class GoodsIssuedConsumer(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock) : IIntegrationEventConsumer<GoodsIssued>
{
    public async Task ConsumeAsync(GoodsIssued integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var line = await db.SalesOrderDetails
            .FirstOrDefaultAsync(x => x.Id == integrationEvent.SalesOrderLineId, cancellationToken)
            .ConfigureAwait(false);
        if (line is null)
        {
            return;
        }

        line.ShippedQty = (int)Math.Min(integrationEvent.TotalQuantity, line.OrderQty);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var order = await db.SalesOrders.FirstAsync(x => x.Id == line.SalesOrderId, cancellationToken).ConfigureAwait(false);
        var allShipped = !await db.SalesOrderDetails
            .AnyAsync(x => x.SalesOrderId == order.Id && x.ShippedQty < x.OrderQty, cancellationToken)
            .ConfigureAwait(false);

        // A cancelled or rejected order stays as it is.
        if (allShipped && order.Status is not ("C" or "R"))
        {
            order.Status = "S";
            order.ShipDate ??= DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

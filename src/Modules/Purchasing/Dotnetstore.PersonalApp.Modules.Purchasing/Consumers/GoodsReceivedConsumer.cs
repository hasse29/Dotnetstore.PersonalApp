using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Consumers;

/// <summary>Sets the line to the total the warehouse has received against it.</summary>
internal sealed class GoodsReceivedConsumer(IDbContextFactory<PurchasingDbContext> dbFactory) : IIntegrationEventConsumer<GoodsReceived>
{
    public async Task ConsumeAsync(GoodsReceived integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var line = await db.PurchaseOrderDetails
            .FirstOrDefaultAsync(x => x.Id == integrationEvent.PurchaseOrderLineId, cancellationToken)
            .ConfigureAwait(false);
        if (line is null)
        {
            return;
        }

        line.ReceivedQty = integrationEvent.TotalQuantity;
        line.StockedQty = line.ReceivedQty - line.RejectedQty;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await OrderTotals.RecalculatePurchaseOrderAsync(db, line.PurchaseOrderId, cancellationToken).ConfigureAwait(false);
    }
}

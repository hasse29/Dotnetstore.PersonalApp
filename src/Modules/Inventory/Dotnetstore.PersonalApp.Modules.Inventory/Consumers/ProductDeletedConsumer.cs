using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Consumers;

/// <summary>Keeps this module's rows in step when a product is deleted in another module.</summary>
internal sealed class ProductDeletedConsumer(IDbContextFactory<InventoryDbContext> dbFactory) : IIntegrationEventConsumer<ProductDeleted>
{
    public async Task ConsumeAsync(ProductDeleted integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.StockLevels.Where(x => x.ProductId == integrationEvent.ProductId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.InventoryCountLines.Where(x => x.ProductId == integrationEvent.ProductId && !x.InventoryCount.Posted).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
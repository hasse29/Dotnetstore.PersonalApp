using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Consumers;

/// <summary>Keeps this module's rows in step when a product is deleted in another module.</summary>
internal sealed class ProductDeletedConsumer(IDbContextFactory<PurchasingDbContext> dbFactory) : IIntegrationEventConsumer<ProductDeleted>
{
    public async Task ConsumeAsync(ProductDeleted integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.ProductVendors.Where(x => x.ProductId == integrationEvent.ProductId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
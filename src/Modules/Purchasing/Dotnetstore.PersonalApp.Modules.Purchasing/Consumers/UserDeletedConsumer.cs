using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Consumers;

/// <summary>Keeps this module's rows in step when a user is deleted in another module.</summary>
internal sealed class UserDeletedConsumer(IDbContextFactory<PurchasingDbContext> dbFactory) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.PurchaseOrders.Where(x => x.BuyerId == integrationEvent.UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.BuyerId, (Guid?)null), cancellationToken).ConfigureAwait(false);
    }
}
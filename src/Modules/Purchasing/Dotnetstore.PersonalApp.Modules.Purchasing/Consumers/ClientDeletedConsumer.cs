using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Consumers;

/// <summary>Keeps this module's rows in step when a client is deleted in another module.</summary>
internal sealed class ClientDeletedConsumer(IDbContextFactory<PurchasingDbContext> dbFactory) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.Vendors.Where(x => x.ClientId == integrationEvent.ClientId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ClientId, (Guid?)null), cancellationToken).ConfigureAwait(false);
    }
}
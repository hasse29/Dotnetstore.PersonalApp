using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Consumers;

/// <summary>Keeps this module's rows in step when a client is deleted in another module.</summary>
internal sealed class ClientDeletedConsumer(IDbContextFactory<SalesDbContext> dbFactory) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.Customers.Where(x => x.ClientId == integrationEvent.ClientId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ClientId, (Guid?)null), cancellationToken).ConfigureAwait(false);
    }
}
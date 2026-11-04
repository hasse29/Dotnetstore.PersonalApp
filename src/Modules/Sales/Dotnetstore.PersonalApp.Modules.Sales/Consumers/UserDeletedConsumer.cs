using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Consumers;

/// <summary>Keeps this module's rows in step when a user is deleted in another module.</summary>
internal sealed class UserDeletedConsumer(IDbContextFactory<SalesDbContext> dbFactory) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await db.SalesPeople.Where(x => x.UserId == integrationEvent.UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, (Guid?)null), cancellationToken).ConfigureAwait(false);
    }
}
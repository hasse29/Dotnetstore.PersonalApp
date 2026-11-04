using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Startup;

internal sealed class MigratePurchasingDatabaseTask(IDbContextFactory<PurchasingDbContext> dbFactory, ILocalizationService localization) : IStartupTask
{
    public string Description => localization.Get("Startup_PurchasingDatabase");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
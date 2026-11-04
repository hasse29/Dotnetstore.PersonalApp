using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Startup;

internal sealed class MigrateProductionDatabaseTask(IDbContextFactory<ProductionDbContext> dbFactory, ILocalizationService localization) : IStartupTask
{
    public string Description => localization.Get("Startup_ProductionDatabase");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
using Dotnetstore.PersonalApp.Modules.Audit.Persistence;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Audit.Startup;

internal sealed class MigrateAuditDatabaseTask(IDbContextFactory<AuditDbContext> dbFactory, AuditWriter writer, ILocalizationService localization) : IStartupTask
{
    public string Description => localization.Get("Startup_AuditDatabase");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        writer.Start();
    }
}

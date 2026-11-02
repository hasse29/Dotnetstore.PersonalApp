using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Startup;

internal sealed class MigrateScheduleDatabaseTask(IDbContextFactory<ScheduleDbContext> dbFactory, ILocalizationService localization) : IStartupTask
{
    public string Description => localization.Get("Startup_ScheduleDatabase");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}

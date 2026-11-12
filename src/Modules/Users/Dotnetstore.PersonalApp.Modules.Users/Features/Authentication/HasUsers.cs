using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;

internal static class HasUsers
{
    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory)
    {
        public Task<bool> HandleAsync(CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(cancellationToken));

        private async Task<bool> HandleCoreAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;

internal static class HasUsers
{
    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory)
    {
        public async Task<bool> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

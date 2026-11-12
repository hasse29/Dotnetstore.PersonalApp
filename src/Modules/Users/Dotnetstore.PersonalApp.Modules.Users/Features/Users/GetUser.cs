using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal static class GetUser
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory)
    {
        public Task<Result<UserDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<UserDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return user is null ? Error.NotFound("user_not_found", "User not found.") : UserDto.From(user);
        }
    }
}

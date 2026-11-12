using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

/// <summary>Reads only the photo, so listing users never drags their pictures along.</summary>
internal static class GetUserPhoto
{
    public sealed record Query(Guid UserId);

    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory)
    {
        public Task<Result<byte[]?>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<byte[]?>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await db.Users.AsNoTracking()
                .Where(x => x.Id == query.UserId)
                .Select(x => new { x.Photo })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return found is null ? Error.NotFound("user_not_found", "User not found.") : Result<byte[]?>.Success(found.Photo);
        }
    }
}

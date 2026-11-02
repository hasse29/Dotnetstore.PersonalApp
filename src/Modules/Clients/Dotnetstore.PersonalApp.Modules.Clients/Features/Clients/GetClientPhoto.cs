using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

/// <summary>Reads only the photo, so listing clients never drags their pictures along.</summary>
internal static class GetClientPhoto
{
    public sealed record Query(Guid ClientId);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory)
    {
        public async Task<Result<byte[]?>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await db.Clients.AsNoTracking()
                .Where(x => x.Id == query.ClientId)
                .Select(x => new { x.Photo })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return found is null ? Error.NotFound("client_not_found", "Client not found.") : Result<byte[]?>.Success(found.Photo);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients;

internal sealed class ClientLookup(IDbContextFactory<ClientsDbContext> dbFactory) : IClientLookup
{
    public async Task<IReadOnlyList<ClientReference>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Clients.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClientReference(c.Id, c.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal static class ListContactPersons
{
    public sealed record Query(Guid ClientId);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<ContactPersonDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.ContactPersons.AsNoTracking()
                .Where(p => p.ClientId == query.ClientId)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new ContactPersonDto(p.Id, p.ClientId, p.FirstName, p.LastName, p.JobTitle, p.Notes, p.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

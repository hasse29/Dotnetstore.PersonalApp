using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal static class ListContactPersons
{
    public sealed record Query(Guid ClientId);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory)
    {
        public Task<IReadOnlyList<ContactPersonDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<IReadOnlyList<ContactPersonDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.ContactPersons.AsNoTracking()
                .Where(p => p.ClientId == query.ClientId)
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .Select(p => new ContactPersonDto(p.Id, p.ClientId, p.FirstName, p.LastName, p.JobTitle, p.Notes, p.CreatedAt, p.PhotoThumbnail))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal static class GetClient
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory)
    {
        public Task<Result<ClientDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<ClientDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var client = await db.Clients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return client is null ? Error.NotFound("client_not_found", "Client not found.") : ClientDto.From(client);
        }
    }
}

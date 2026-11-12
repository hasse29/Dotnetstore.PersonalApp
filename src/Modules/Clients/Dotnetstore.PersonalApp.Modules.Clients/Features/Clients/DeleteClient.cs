using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal static class DeleteClient
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory, IEventPublisher events)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // The contact persons go with the client (cascade), so their ids are read first to tell the other modules.
            var contactPersonIds = await db.ContactPersons
                .Where(p => p.ClientId == command.Id)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var deleted = await db.Clients
                .Where(c => c.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("client_not_found", "Client not found.");
            }

            foreach (var contactPersonId in contactPersonIds)
            {
                await events.PublishAsync(new ContactPersonDeleted(contactPersonId), cancellationToken).ConfigureAwait(false);
            }

            await events.PublishAsync(new ClientDeleted(command.Id), cancellationToken).ConfigureAwait(false);

            return command.Id;
        }
    }
}

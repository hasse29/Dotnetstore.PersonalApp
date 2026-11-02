using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal static class DeleteContactPerson
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory, IEventPublisher events)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.ContactPersons
                .Where(p => p.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("contact_person_not_found", "Contact person not found.");
            }

            await events.PublishAsync(new ContactPersonDeleted(command.Id), cancellationToken).ConfigureAwait(false);

            return command.Id;
        }
    }
}

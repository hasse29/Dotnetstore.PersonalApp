using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal static class DeleteStudentsForClient
{
    public sealed record Command(Guid ClientId);

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory, IEventPublisher events)
    {
        public async Task<int> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // The ids are read first so the other modules can remove each student's addresses, phone numbers and email addresses.
            var ids = await db.Students
                .Where(s => s.ClientId == command.ClientId)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            await db.Students
                .Where(s => s.ClientId == command.ClientId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var id in ids)
            {
                await events.PublishAsync(new StudentDeleted(id), cancellationToken).ConfigureAwait(false);
            }

            return ids.Count;
        }
    }
}

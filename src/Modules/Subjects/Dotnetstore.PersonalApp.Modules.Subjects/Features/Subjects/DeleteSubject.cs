using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal static class DeleteSubject
{
    public sealed record Command(Guid Id);

    /// <summary>The subject is also taken off every teacher who taught it (cascade); other modules hear about it through <see cref="SubjectDeleted"/>.</summary>
    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory, IEventPublisher events)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.Subjects
                .Where(s => s.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("subject_not_found", "Subject not found.");
            }

            await events.PublishAsync(new SubjectDeleted(command.Id), cancellationToken).ConfigureAwait(false);

            return command.Id;
        }
    }
}

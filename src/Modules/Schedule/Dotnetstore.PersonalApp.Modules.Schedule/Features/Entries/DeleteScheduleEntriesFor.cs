using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

/// <summary>Cleans up after another module deleted a teacher, a school or a subject that lessons refer to.</summary>
internal static class DeleteScheduleEntriesFor
{
    public sealed record Command(Guid? UserId = null, Guid? ClientId = null, Guid? SubjectId = null);

    public sealed class Handler(IDbContextFactory<ScheduleDbContext> dbFactory)
    {
        public async Task HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            await db.ScheduleEntries
                .Where(e => (command.UserId != null && e.UserId == command.UserId)
                    || (command.ClientId != null && e.ClientId == command.ClientId)
                    || (command.SubjectId != null && e.SubjectId == command.SubjectId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

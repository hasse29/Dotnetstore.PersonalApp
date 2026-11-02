using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

/// <summary>Takes a deleted student off every lesson they attended; the lessons themselves stay.</summary>
internal static class RemoveStudentFromEntries
{
    public sealed record Command(Guid StudentId);

    public sealed class Handler(IDbContextFactory<ScheduleDbContext> dbFactory)
    {
        public async Task HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            await db.ScheduleEntryStudents
                .Where(s => s.StudentId == command.StudentId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

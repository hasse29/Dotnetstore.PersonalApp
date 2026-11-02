using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal static class DeleteScheduleEntry
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<ScheduleDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.ScheduleEntries
                .Where(e => e.Id == command.Id && e.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("schedule_entry_not_found", "Lesson not found.") : command.Id;
        }
    }
}

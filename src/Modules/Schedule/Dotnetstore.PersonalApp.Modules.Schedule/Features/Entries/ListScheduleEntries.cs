using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal static class ListScheduleEntries
{
    public sealed record Query(Guid UserId);

    public sealed class Handler(IDbContextFactory<ScheduleDbContext> dbFactory)
    {
        /// <summary>One teacher's lessons in week order: Monday first, Sunday last, then by start time.</summary>
        public async Task<IReadOnlyList<ScheduleEntryDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entries = await db.ScheduleEntries.AsNoTracking()
                .Include(e => e.Students)
                .Where(e => e.UserId == query.UserId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Sorted in memory: DayOfWeek counts from Sunday, and one teacher has few lessons.
            return [.. entries
                .OrderBy(e => ((int)e.Day + 6) % 7)
                .ThenBy(e => e.StartTime)
                .Select(ScheduleEntryDto.From)];
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal static class ListTests
{
    public sealed record Query(Guid UserId);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        /// <summary>The teacher's tests, the latest first: by the day taken, then by when it was recorded.</summary>
        public async Task<IReadOnlyList<TestDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var tests = await db.Tests.AsNoTracking()
                .Where(t => t.UserId == query.UserId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var counts = await db.TestResults.AsNoTracking()
                .Where(r => r.Test.UserId == query.UserId)
                .GroupBy(r => r.TestId)
                .Select(g => new { TestId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.TestId, g => g.Count, cancellationToken)
                .ConfigureAwait(false);

            // Ordered in memory: SQLite cannot order DateTimeOffset values server-side.
            return [.. tests
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => TestDto.From(t, counts.GetValueOrDefault(t.Id)))];
        }
    }
}

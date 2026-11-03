using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

internal static class ListGrades
{
    /// <param name="UserId">The teacher whose grades are listed.</param>
    /// <param name="StudentId">One student, or null for every student.</param>
    public sealed record Query(Guid UserId, Guid? StudentId);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        /// <summary>The latest grade first: by the date it was given, then by when it was recorded.</summary>
        public async Task<IReadOnlyList<GradeDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var grades = await db.Grades.AsNoTracking()
                .Include(g => g.ScaleItem)
                .Where(g => g.UserId == query.UserId && (query.StudentId == null || g.StudentId == query.StudentId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var scaleNames = await db.GradeScales.AsNoTracking()
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken)
                .ConfigureAwait(false);

            // Ordered in memory: SQLite cannot order DateTimeOffset values server-side.
            return [.. grades
                .OrderByDescending(g => g.Date)
                .ThenByDescending(g => g.CreatedAt)
                .Select(g => GradeDto.From(g, g.ScaleItem, scaleNames.GetValueOrDefault(g.ScaleItem.ScaleId, "?")))];
        }
    }
}

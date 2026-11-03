using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal static class ListGradeScales
{
    public sealed record Query;

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<GradeScaleDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var scales = await db.GradeScales.AsNoTracking()
                .Include(s => s.Items)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return [.. scales.Select(GradeScaleDto.From)];
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal static class ListGradeScales
{
    public sealed record Query;

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        public Task<IReadOnlyList<GradeScaleDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<IReadOnlyList<GradeScaleDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
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

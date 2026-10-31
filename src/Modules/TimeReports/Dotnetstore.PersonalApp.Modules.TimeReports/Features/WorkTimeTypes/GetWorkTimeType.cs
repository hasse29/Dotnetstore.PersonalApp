using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal static class GetWorkTimeType
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory)
    {
        public async Task<Result<WorkTimeTypeDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.WorkTimeTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return type is null ? Error.NotFound("work_time_type_not_found", "Work time type not found.") : WorkTimeTypeDto.From(type);
        }
    }
}

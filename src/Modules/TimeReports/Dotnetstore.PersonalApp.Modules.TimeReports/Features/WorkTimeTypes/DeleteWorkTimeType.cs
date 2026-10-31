using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal static class DeleteWorkTimeType
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.TimeReports.AnyAsync(r => r.WorkTimeTypeId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("work_time_type_in_use", "The work time type is used by existing time reports.");
            }

            var deleted = await db.WorkTimeTypes
                .Where(t => t.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("work_time_type_not_found", "Work time type not found.") : command.Id;
        }
    }
}

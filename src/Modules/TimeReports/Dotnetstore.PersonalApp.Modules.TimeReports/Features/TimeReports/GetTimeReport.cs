using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

internal static class GetTimeReport
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IClientLookup clients)
    {
        public async Task<Result<TimeReportDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var report = await db.TimeReports.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == query.Id && r.UserId == query.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (report is null)
            {
                return Error.NotFound("time_report_not_found", "Time report not found.");
            }

            var typeName = await db.WorkTimeTypes.AsNoTracking()
                .Where(t => t.Id == report.WorkTimeTypeId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            var name = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(c => c.Id == report.ClientId)?.Name;
            return TimeReportDto.From(report, name, typeName);
        }
    }
}
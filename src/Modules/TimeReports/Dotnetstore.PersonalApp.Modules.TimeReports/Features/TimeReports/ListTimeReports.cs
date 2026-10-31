using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>Lists one user's reports, newest first, optionally for a single client. Totals cover every match, not just the page.</summary>
internal static class ListTimeReports
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, Guid? ClientId = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<TimeReportDto> Items, int TotalCount, decimal TotalHours, decimal TotalKilometers);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IClientLookup clients)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var reports = db.TimeReports.AsNoTracking().Where(r => r.UserId == query.UserId);
            if (query.ClientId is { } clientId)
            {
                reports = reports.Where(r => r.ClientId == clientId);
            }

            // SQLite cannot sum decimals in SQL, so the (small) set of figures is summed here.
            var figures = await reports
                .Select(r => new { r.TotalHours, r.DrivingKilometers })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var names = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(c => c.Id, c => c.Name);

            var rows = await reports
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var typeNames = await db.WorkTimeTypes.AsNoTracking()
                .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken)
                .ConfigureAwait(false);

            var items = rows.ConvertAll(r => TimeReportDto.From(r, names.GetValueOrDefault(r.ClientId), typeNames.GetValueOrDefault(r.WorkTimeTypeId)));

            return new Response(items, figures.Count, figures.Sum(f => f.TotalHours), figures.Sum(f => f.DrivingKilometers));
        }
    }
}
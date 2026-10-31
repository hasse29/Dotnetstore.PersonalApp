using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal static class ListWorkTimeTypes
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<WorkTimeTypeDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var types = db.WorkTimeTypes.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                types = types.Where(t =>
                    EF.Functions.Like(t.Name, pattern) ||
                    (t.Description != null && EF.Functions.Like(t.Description, pattern)));
            }

            var total = await types.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await types
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new WorkTimeTypeDto(t.Id, t.Name, t.Description, t.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}

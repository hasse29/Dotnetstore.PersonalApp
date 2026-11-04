using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;

internal static class ListSalesPersonQuotaHistories
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<SalesPersonQuotaHistoryDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.SalesPersonQuotaHistories.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.SalesPersonId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.SalesPerson.Name, pattern));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderBy(t => t.SalesPerson.Name)
                .ThenByDescending(t => t.QuotaDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(SalesPersonQuotaHistoryDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
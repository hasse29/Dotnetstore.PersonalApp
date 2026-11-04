using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal static class ListWorkOrders
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<WorkOrderDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.WorkOrders.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Number, pattern) ||
                    EF.Functions.Like(t.Product.Name, pattern) ||
                    (t.ScrapReason != null && EF.Functions.Like(t.ScrapReason.Name, pattern)));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderByDescending(t => t.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(WorkOrderDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
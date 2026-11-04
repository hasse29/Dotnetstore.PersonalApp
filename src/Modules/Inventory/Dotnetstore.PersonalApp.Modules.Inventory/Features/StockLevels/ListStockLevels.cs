using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;

internal static class ListStockLevels
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<StockLevelDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.StockLevels.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.LocationId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Location.Code, pattern));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderBy(t => t.Location.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(StockLevelDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var productNames = await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<StockLevelDto> named = [.. items.Select(i => i.WithNames(productNames))];

            return new Response(named, total);
        }
    }
}
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

internal static class ListStockMovements
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<StockMovementDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.StockMovements.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.LocationId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    (t.Reference != null && EF.Functions.Like(t.Reference, pattern)) ||
                    EF.Functions.Like(t.Location.Code, pattern));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(StockMovementDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var productNames = await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<StockMovementDto> named = [.. items.Select(i => i.WithNames(productNames))];

            return new Response(named, total);
        }
    }
}
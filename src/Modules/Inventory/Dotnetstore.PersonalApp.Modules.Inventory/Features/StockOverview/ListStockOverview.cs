using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockOverview;

/// <summary>One product at one location.</summary>
internal sealed record StockOverviewRow(Guid ProductId, string ProductName, string WarehouseName, string LocationCode, decimal Quantity, decimal? MinQuantity)
{
    /// <summary>At or below the reorder level; a product without a level is never low.</summary>
    public bool IsLow => MinQuantity is { } min && Quantity <= min;
}

internal static class ListStockOverview
{
    public const int MaxRows = 500;

    public sealed record Query(string? Search = null, Guid? WarehouseId = null, bool OnlyLow = false);

    public sealed record Response(IReadOnlyList<StockOverviewRow> Rows, int TotalCount);

    /// <summary>Where each product is stored and how many there are; the product names come from the Production module, so the search runs here.</summary>
    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var names = await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false);

            var levels = db.StockLevels.AsNoTracking();
            if (query.WarehouseId is { } warehouseId)
            {
                levels = levels.Where(x => x.Location.WarehouseId == warehouseId);
            }

            if (query.OnlyLow)
            {
                levels = levels.Where(x => x.MinQuantity != null && x.Quantity <= x.MinQuantity);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                var pattern = $"%{search}%";
                var matching = names.Where(x => x.Value.Contains(search, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList();
                levels = levels.Where(x => matching.Contains(x.ProductId) || EF.Functions.Like(x.Location.Code, pattern));
            }

            var total = await levels.CountAsync(cancellationToken).ConfigureAwait(false);

            // Only the first products by name are read in full, so a big warehouse costs one small count query and one bounded one.
            var perProduct = await levels
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var chosen = new List<Guid>();
            var running = 0;
            foreach (var product in perProduct.OrderBy(x => names.GetValueOrDefault(x.ProductId, string.Empty), StringComparer.CurrentCultureIgnoreCase))
            {
                if (running >= MaxRows)
                {
                    break;
                }

                chosen.Add(product.ProductId);
                running += product.Count;
            }

            var rows = await levels
                .Where(x => chosen.Contains(x.ProductId))
                .Select(x => new { x.ProductId, Warehouse = x.Location.Warehouse.Name, x.Location.Code, x.Quantity, x.MinQuantity })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var result = rows
                .Select(x => new StockOverviewRow(x.ProductId, names.GetValueOrDefault(x.ProductId, string.Empty), x.Warehouse, x.Code, x.Quantity, x.MinQuantity))
                .OrderBy(x => x.ProductName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.LocationCode, StringComparer.Ordinal)
                .Take(MaxRows)
                .ToList();

            return new Response(result, total);
        }
    }
}

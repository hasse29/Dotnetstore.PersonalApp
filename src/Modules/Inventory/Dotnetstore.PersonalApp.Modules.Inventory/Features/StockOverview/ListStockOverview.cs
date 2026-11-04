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

            var levels = db.StockLevels.AsNoTracking();
            if (query.WarehouseId is { } warehouseId)
            {
                levels = levels.Where(x => x.Location.WarehouseId == warehouseId);
            }

            var rows = await levels
                .Select(x => new { x.ProductId, Warehouse = x.Location.Warehouse.Name, x.Location.Code, x.Quantity, x.MinQuantity })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var names = await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false);

            var result = rows
                .Select(x => new StockOverviewRow(x.ProductId, names.GetValueOrDefault(x.ProductId, string.Empty), x.Warehouse, x.Code, x.Quantity, x.MinQuantity))
                .Where(x => string.IsNullOrWhiteSpace(query.Search)
                    || x.ProductName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)
                    || x.LocationCode.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(x => !query.OnlyLow || x.IsLow)
                .OrderBy(x => x.ProductName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.LocationCode, StringComparer.Ordinal)
                .ToList();

            return new Response([.. result.Take(MaxRows)], result.Count);
        }
    }
}

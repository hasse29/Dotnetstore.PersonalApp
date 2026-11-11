using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

internal sealed record ProductStockLocationDto(Guid LocationId, string LocationName, decimal Quantity);

/// <summary>One product's stock broken down by location; locations with nothing left are left out.</summary>
internal sealed record ProductStockDto(Guid ProductId, decimal Total, decimal? MinimumStock, IReadOnlyList<ProductStockLocationDto> Locations)
{
    public bool IsLow => MinimumStock is { } minimum && Total <= minimum;
}

internal static class GetProductStock
{
    public sealed record Query(Guid ProductId);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<ProductStockDto> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var minimum = await db.Products.AsNoTracking()
                .Where(p => p.Id == query.ProductId)
                .Select(p => p.MinimumStock)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var levels = await db.StockLevels.AsNoTracking()
                .Where(l => l.ProductId == query.ProductId)
                .Join(
                    db.StorageLocations,
                    l => l.LocationId,
                    s => s.Id,
                    (l, s) => new ProductStockLocationDto(s.Id, s.Name, l.Quantity))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var locations = levels
                .Where(l => l.Quantity != 0m)
                .OrderBy(l => l.LocationName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new ProductStockDto(query.ProductId, levels.Sum(l => l.Quantity), minimum, locations);
        }
    }
}

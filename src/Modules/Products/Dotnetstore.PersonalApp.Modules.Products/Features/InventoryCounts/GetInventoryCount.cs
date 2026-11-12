using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

internal static class GetInventoryCount
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Result<InventoryCountDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<InventoryCountDetailDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var count = await db.InventoryCounts.AsNoTracking()
                .Where(c => c.Id == query.Id)
                .Select(c => new InventoryCountDto(
                    c.Id,
                    c.Name,
                    c.LocationId,
                    db.StorageLocations.Where(l => l.Id == c.LocationId).Select(l => l.Name).FirstOrDefault(),
                    c.Note,
                    c.CreatedAt,
                    c.PostedAt,
                    c.Lines.Count,
                    c.Lines.Count(l => l.CountedQuantity != null)))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (count is null)
            {
                return Error.NotFound("inventory_count_not_found", "Inventory count not found.");
            }

            var lines = await db.InventoryCountLines.AsNoTracking()
                .Where(l => l.CountId == query.Id)
                .Select(l => new InventoryCountLineDto(
                    l.Id,
                    l.ProductId,
                    db.Products.Where(p => p.Id == l.ProductId).Select(p => p.Number).First(),
                    db.Products.Where(p => p.Id == l.ProductId).Select(p => p.Name).First(),
                    db.Products.Where(p => p.Id == l.ProductId).Select(p => p.Unit).First(),
                    l.LocationId,
                    db.StorageLocations.Where(s => s.Id == l.LocationId).Select(s => s.Name).First(),
                    l.ExpectedQuantity,
                    l.CountedQuantity))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new InventoryCountDetailDto(
                count,
                [.. lines.OrderBy(l => l.LocationName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(l => l.ProductName, StringComparer.CurrentCultureIgnoreCase)]);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;

/// <summary>Every supplier's price for one product, cheapest first.</summary>
internal static class ListSupplierPrices
{
    public sealed record Query(Guid ProductId);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<IReadOnlyList<SupplierPriceDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<IReadOnlyList<SupplierPriceDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var prices = await db.SupplierPrices.AsNoTracking()
                .Where(p => p.ProductId == query.ProductId)
                .Join(
                    db.Suppliers,
                    p => p.SupplierId,
                    s => s.Id,
                    (p, s) => new SupplierPriceDto(p.Id, p.SupplierId, s.Name, s.IsActive, p.ProductId, p.SupplierArticleNumber, p.Price, p.UpdatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // SQLite cannot order by decimal in SQL, so the sort happens here.
            return [.. prices.OrderBy(p => p.Price).ThenBy(p => p.SupplierName, StringComparer.CurrentCultureIgnoreCase)];
        }
    }
}

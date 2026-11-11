using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Products;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products;

internal sealed class ProductCatalog(IDbContextFactory<ProductsDbContext> dbFactory) : IProductCatalog
{
    public async Task<IReadOnlyList<ProductSummary>> SearchAsync(string? search, int maxCount, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var products = db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            products = products.Where(p => EF.Functions.Like(p.Number, pattern) || EF.Functions.Like(p.Name, pattern));
        }

        var rows = await products
            .OrderBy(p => p.Name)
            .Take(Math.Clamp(maxCount, 1, 100))
            .Select(p => new { p.Id, p.Number, p.Name, p.Unit, p.UnitPrice, p.VatRate })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(p => new ProductSummary(p.Id, p.Number, p.Name, p.Unit.ToString(), p.UnitPrice, p.VatRate))];
    }
}

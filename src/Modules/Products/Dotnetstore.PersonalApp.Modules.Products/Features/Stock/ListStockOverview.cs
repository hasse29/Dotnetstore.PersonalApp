using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>One product's total stock across all locations.</summary>
internal sealed record StockOverviewDto(Guid ProductId, string Number, string Name, ProductUnit Unit, decimal Total, decimal? MinimumStock)
{
    /// <summary>At or below the product's minimum; products without a minimum are never low.</summary>
    public bool IsLow => MinimumStock is { } minimum && Total <= minimum;
}

/// <summary>Every product that can be kept in stock with how many there are in total, so products with none show up too.</summary>
internal static class ListStockOverview
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, bool LowOnly = false, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<StockOverviewDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var products = db.Products.AsNoTracking().Where(p => p.Kind != ProductKind.Consulting);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                products = products.Where(p =>
                    EF.Functions.Like(p.Number, pattern) ||
                    EF.Functions.Like(p.Name, pattern) ||
                    (p.Manufacturer != null && EF.Functions.Like(p.Manufacturer, pattern)) ||
                    (p.Model != null && EF.Functions.Like(p.Model, pattern)));
            }

            var rows = await products
                .Select(p => new { p.Id, p.Number, p.Name, p.Unit, p.MinimumStock, p.IsActive })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // SQLite cannot sum decimals in SQL, and a solo catalogue is small, so the totals are added up here.
            var totals = (await db.StockLevels.AsNoTracking()
                    .Select(l => new { l.ProductId, l.Quantity })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .GroupBy(l => l.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

            var items = rows
                .Select(p => (Product: p, Total: totals.GetValueOrDefault(p.Id)))
                .Where(x => x.Product.IsActive || x.Total != 0m)
                .Select(x => new StockOverviewDto(x.Product.Id, x.Product.Number, x.Product.Name, x.Product.Unit, x.Total, x.Product.MinimumStock))
                .Where(s => !query.LowOnly || s.IsLow)
                .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(s => s.Number, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new Response([.. items.Skip((page - 1) * pageSize).Take(pageSize)], items.Count);
        }
    }
}

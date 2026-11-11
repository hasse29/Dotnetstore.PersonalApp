using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>Lists products by name, optionally of one kind and matching a search on number, name, manufacturer or model.</summary>
internal static class ListProducts
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, ProductKind? Kind = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<ProductDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var products = db.Products.AsNoTracking();
            if (query.Kind is { } kind)
            {
                products = products.Where(p => p.Kind == kind);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                products = products.Where(p =>
                    EF.Functions.Like(p.Number, pattern) ||
                    EF.Functions.Like(p.Name, pattern) ||
                    (p.Manufacturer != null && EF.Functions.Like(p.Manufacturer, pattern)) ||
                    (p.Model != null && EF.Functions.Like(p.Model, pattern)));
            }

            var total = await products.CountAsync(cancellationToken).ConfigureAwait(false);

            // Projected column by column so the full-size photos are never loaded for a list.
            var rows = await products
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Number)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(
                    p.Id,
                    p.Number,
                    p.Name,
                    p.Kind,
                    p.CategoryId,
                    db.ProductCategories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).FirstOrDefault(),
                    p.Description,
                    p.IsActive,
                    p.Unit,
                    p.UnitPrice,
                    p.CostPrice,
                    p.VatRate,
                    p.Manufacturer,
                    p.Model,
                    p.SoftwareVersion,
                    p.MinimumHours,
                    p.MinimumStock,
                    p.PhotoThumbnail))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(rows, total);
        }
    }
}

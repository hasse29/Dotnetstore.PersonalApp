using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal static class ListProducts
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<ProductDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Name, pattern) ||
                    EF.Functions.Like(t.ProductNumber, pattern) ||
                    (t.Color != null && EF.Functions.Like(t.Color, pattern)) ||
                    (t.Size != null && EF.Functions.Like(t.Size, pattern)) ||
                    (t.SizeUnit != null && EF.Functions.Like(t.SizeUnit.Name, pattern)) ||
                    (t.WeightUnit != null && EF.Functions.Like(t.WeightUnit.Name, pattern)) ||
                    (t.Subcategory != null && EF.Functions.Like(t.Subcategory.Name, pattern)) ||
                    (t.Model != null && EF.Functions.Like(t.Model.Name, pattern)));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ProductDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
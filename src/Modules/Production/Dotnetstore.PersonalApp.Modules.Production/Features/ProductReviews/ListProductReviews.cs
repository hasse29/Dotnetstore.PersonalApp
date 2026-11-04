using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

internal static class ListProductReviews
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<ProductReviewDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.ProductReviews.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.ProductId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.ReviewerName, pattern) ||
                    EF.Functions.Like(t.EmailAddress, pattern) ||
                    EF.Functions.Like(t.Product.Name, pattern));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderByDescending(t => t.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ProductReviewDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
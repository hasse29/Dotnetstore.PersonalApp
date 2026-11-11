using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>The counts, newest first.</summary>
internal static class ListInventoryCounts
{
    public const int MaxPageSize = 50;

    public sealed record Query(int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<InventoryCountDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var total = await db.InventoryCounts.CountAsync(cancellationToken).ConfigureAwait(false);

            // Ids are time-ordered (version 7), and SQLite cannot order by DateTimeOffset.
            var items = await db.InventoryCounts.AsNoTracking()
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}

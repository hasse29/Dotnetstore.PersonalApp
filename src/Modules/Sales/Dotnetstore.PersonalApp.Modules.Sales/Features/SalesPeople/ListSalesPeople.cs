using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal static class ListSalesPeople
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<SalesPersonDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.SalesPeople.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Name, pattern) ||
                    (t.Territory != null && EF.Functions.Like(t.Territory.Name, pattern)));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(SalesPersonDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var userNames = await references.UserNamesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<SalesPersonDto> named = [.. items.Select(i => i.WithNames(userNames))];

            return new Response(named, total);
        }
    }
}
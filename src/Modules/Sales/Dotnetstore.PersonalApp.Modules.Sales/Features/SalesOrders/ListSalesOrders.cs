using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal static class ListSalesOrders
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<SalesOrderDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.SalesOrders.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.CustomerId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Number, pattern) ||
                    (t.PurchaseOrderNumber != null && EF.Functions.Like(t.PurchaseOrderNumber, pattern)) ||
                    EF.Functions.Like(t.Customer.Name, pattern) ||
                    (t.SalesPerson != null && EF.Functions.Like(t.SalesPerson.Name, pattern)) ||
                    (t.Territory != null && EF.Functions.Like(t.Territory.Name, pattern)));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderByDescending(t => t.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(SalesOrderDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var shipMethodNames = await references.ShipMethodNamesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<SalesOrderDto> named = [.. items.Select(i => i.WithNames(shipMethodNames))];

            return new Response(named, total);
        }
    }
}
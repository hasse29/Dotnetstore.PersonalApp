using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal static class ListPurchaseOrders
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<PurchaseOrderDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.PurchaseOrders.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.VendorId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Number, pattern) ||
                    EF.Functions.Like(t.Vendor.Name, pattern) ||
                    EF.Functions.Like(t.ShipMethod.Name, pattern));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderByDescending(t => t.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(PurchaseOrderDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var userNames = await references.UserNamesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<PurchaseOrderDto> named = [.. items.Select(i => i.WithNames(userNames))];

            return new Response(named, total);
        }
    }
}
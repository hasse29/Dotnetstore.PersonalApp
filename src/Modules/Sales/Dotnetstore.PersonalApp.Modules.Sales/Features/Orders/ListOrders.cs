using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

internal static class ListOrders
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, OrderStatus? Status = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<OrderDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var orders = db.Orders.AsNoTracking();
            if (query.Status is { } status)
            {
                orders = orders.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                orders = orders.Where(o => EF.Functions.Like(o.Number, pattern) || EF.Functions.Like(o.CustomerName, pattern) || EF.Functions.Like(o.OfferNumber, pattern));
            }

            var total = await orders.CountAsync(cancellationToken).ConfigureAwait(false);

            // The newest first: the id is a version 7 Guid, so it sorts by creation time.
            var rows = await orders
                .OrderByDescending(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.Lines)
                .Include(o => o.PickList!).ThenInclude(p => p.Lines)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = rows
                .Select(o => OrderDetailDto.ToDto(o, OfferTotals.Compute(o.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross))
                .ToList();

            return new Response(items, total);
        }
    }
}

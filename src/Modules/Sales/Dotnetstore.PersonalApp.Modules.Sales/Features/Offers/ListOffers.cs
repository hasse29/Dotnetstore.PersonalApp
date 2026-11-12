using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal static class ListOffers
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, OfferStatus? Status = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<OfferDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offers = db.Offers.AsNoTracking();
            if (query.Status is { } status)
            {
                offers = offers.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                offers = offers.Where(o => EF.Functions.Like(o.Number, pattern) || EF.Functions.Like(o.CustomerName, pattern));
            }

            var total = await offers.CountAsync(cancellationToken).ConfigureAwait(false);

            // The newest first. The id is a version 7 Guid, so it sorts by creation time; SQLite cannot sort a DateOnly column by an expression.
            var rows = await offers
                .OrderByDescending(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.Lines)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Decimals are summed in memory: SQLite has no decimal type to add up.
            var items = rows
                .Select(o => OfferDetailDto.ToDto(o, OfferTotals.Compute(o.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross, today))
                .ToList();

            return new Response(items, total);
        }
    }
}

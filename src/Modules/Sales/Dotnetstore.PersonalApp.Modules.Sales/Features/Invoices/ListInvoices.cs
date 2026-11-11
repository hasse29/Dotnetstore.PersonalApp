using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

internal static class ListInvoices
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, InvoiceStatus? Status = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<InvoiceDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoices = db.Invoices.AsNoTracking();
            if (query.Status is { } status)
            {
                invoices = invoices.Where(i => i.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                invoices = invoices.Where(i => EF.Functions.Like(i.Number, pattern) || EF.Functions.Like(i.CustomerName, pattern) || EF.Functions.Like(i.OrderNumber, pattern));
            }

            var total = await invoices.CountAsync(cancellationToken).ConfigureAwait(false);

            // The newest first: the id is a version 7 Guid, so it sorts by creation time.
            var rows = await invoices
                .OrderByDescending(i => i.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = rows
                .Select(i => InvoiceDetailDto.ToDto(i, OfferTotals.Compute(i.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross, today))
                .ToList();

            return new Response(items, total);
        }
    }
}

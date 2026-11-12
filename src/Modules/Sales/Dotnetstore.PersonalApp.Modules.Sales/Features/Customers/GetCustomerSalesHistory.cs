using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal enum SalesHistoryKind
{
    Offer,
    Order,
    Invoice,
}

/// <summary>
/// One document of a customer. <paramref name="Status"/> is the name of the offer, order or invoice status, <paramref name="From"/> the
/// number of the document it was made from (empty for an offer), and <paramref name="Paid"/> is only meaningful for an invoice.
/// </summary>
internal sealed record SalesHistoryEntry(
    SalesHistoryKind Kind,
    Guid Id,
    string Number,
    string From,
    DateOnly Date,
    string Status,
    decimal Total,
    decimal Paid);

/// <summary>What the customer has been invoiced. A cancelled invoice counts for nothing.</summary>
internal sealed record SalesHistorySummary(int Offers, int Orders, int Invoices, decimal Invoiced, decimal Paid)
{
    public decimal Outstanding => Math.Max(Invoiced - Paid, 0m);
}

/// <summary>Everything the customer has been offered, ordered and invoiced, newest first.</summary>
internal static class GetCustomerSalesHistory
{
    public sealed record Query(Guid CustomerId);

    public sealed record Response(IReadOnlyList<SalesHistoryEntry> Entries, SalesHistorySummary Summary);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var offers = await db.Offers.AsNoTracking().Include(o => o.Lines)
                .Where(o => o.CustomerId == query.CustomerId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var orders = await db.Orders.AsNoTracking().Include(o => o.Lines)
                .Where(o => o.CustomerId == query.CustomerId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).Include(i => i.Payments)
                .Where(i => i.CustomerId == query.CustomerId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            // Decimals are summed in memory: SQLite has no decimal type to add up.
            var entries = new List<SalesHistoryEntry>();
            entries.AddRange(offers.Select(o => new SalesHistoryEntry(
                SalesHistoryKind.Offer, o.Id, o.Number, string.Empty, o.Date, o.Status.ToString(),
                OfferTotals.Compute(o.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross, 0m)));
            entries.AddRange(orders.Select(o => new SalesHistoryEntry(
                SalesHistoryKind.Order, o.Id, o.Number, o.OfferNumber, o.Date, o.Status.ToString(),
                OfferTotals.Compute(o.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross, 0m)));
            entries.AddRange(invoices.Select(i => new SalesHistoryEntry(
                SalesHistoryKind.Invoice, i.Id, i.Number, i.OrderNumber, i.Date, i.Status.ToString(),
                OfferTotals.Compute(i.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross,
                InvoicePayments.Paid(i))));

            var live = entries.Where(e => e.Kind == SalesHistoryKind.Invoice && e.Status != nameof(InvoiceStatus.Cancelled)).ToList();
            var summary = new SalesHistorySummary(offers.Count, orders.Count, invoices.Count, live.Sum(e => e.Total), live.Sum(e => e.Paid));

            // Newest first. Within a day a later stage comes first (an invoice follows its order, which follows its offer), then the higher number.
            var sorted = entries
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Kind)
                .ThenByDescending(e => e.Number, StringComparer.Ordinal)
                .ToList();

            return new Response(sorted, summary);
        }
    }
}

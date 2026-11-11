using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>What has been paid on an invoice and what a customer has in credit; shared by the payment handlers.</summary>
internal static class InvoicePayments
{
    public const decimal MaxAmount = 10_000_000m;

    public static decimal Total(Invoice invoice) =>
        OfferTotals.Compute(invoice.Lines.Select(l => new LineAmounts(l.Quantity, l.UnitPrice, l.VatRate, l.DiscountPercent))).Gross;

    public static decimal Paid(Invoice invoice) => invoice.Payments.Sum(p => p.Amount);

    public static decimal Outstanding(Invoice invoice) => Math.Max(Total(invoice) - Paid(invoice), 0m);

    /// <summary>Sets the status from the payments. A cancelled invoice stays cancelled.</summary>
    public static void Refresh(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return;
        }

        var paid = Paid(invoice);
        invoice.Status = paid >= Total(invoice) ? InvoiceStatus.Paid : paid > 0m ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;
    }

    /// <summary>What the customer has in credit. Summed in memory: SQLite cannot sum a decimal column.</summary>
    public static async Task<decimal> CreditBalanceAsync(SalesDbContext db, Guid customerId, CancellationToken cancellationToken) =>
        (await db.CreditEntries.AsNoTracking()
            .Where(e => e.CustomerId == customerId)
            .Select(e => e.Amount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false)).Sum();

    public static async Task<Invoice?> LoadAsync(SalesDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Invoices.Include(i => i.Lines).Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);

    public static async Task<InvoiceDetailDto> DetailAsync(SalesDbContext db, Invoice invoice, DateOnly today, CancellationToken cancellationToken) =>
        InvoiceDetailDto.From(invoice, today, await CreditBalanceAsync(db, invoice.CustomerId, cancellationToken).ConfigureAwait(false));
}

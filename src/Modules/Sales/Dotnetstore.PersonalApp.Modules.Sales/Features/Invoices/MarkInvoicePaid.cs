using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>Records that an unpaid invoice has been paid, on the given date or today.</summary>
internal static class MarkInvoicePaid
{
    public sealed record Command(Guid Id, DateOnly? PaidDate = null);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<InvoiceDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (invoice is null)
            {
                return Error.NotFound("invoice_not_found", "Invoice not found.");
            }

            if (invoice.Status != InvoiceStatus.Unpaid)
            {
                return Error.Conflict("invoice_not_unpaid", "Only an unpaid invoice can be marked as paid.");
            }

            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            var paidDate = command.PaidDate ?? today;
            if (paidDate < invoice.Date)
            {
                return Error.Validation("invoice_paid_date_invalid", "The invoice cannot be paid before its date.");
            }

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = paidDate;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return InvoiceDetailDto.From(invoice, today);
        }
    }
}

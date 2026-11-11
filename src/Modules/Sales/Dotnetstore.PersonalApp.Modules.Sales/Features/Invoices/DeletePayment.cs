using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>
/// Removes a payment that was entered by mistake. Credit that came from it is taken back, which is only possible while the customer
/// still has that much; credit that was used for it is given back.
/// </summary>
internal static class DeletePayment
{
    public sealed record Command(Guid InvoiceId, Guid PaymentId);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<InvoiceDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoice = await InvoicePayments.LoadAsync(db, command.InvoiceId, cancellationToken).ConfigureAwait(false);
            if (invoice is null)
            {
                return Error.NotFound("invoice_not_found", "Invoice not found.");
            }

            var payment = invoice.Payments.FirstOrDefault(p => p.Id == command.PaymentId);
            if (payment is null)
            {
                return Error.NotFound("payment_not_found", "Payment not found.");
            }

            if (payment.Excess > 0m)
            {
                var balance = await InvoicePayments.CreditBalanceAsync(db, invoice.CustomerId, cancellationToken).ConfigureAwait(false);
                if (balance < payment.Excess)
                {
                    return Error.Conflict("credit_insufficient", "The credit from this payment has already been used or paid back.");
                }
            }

            var entries = await db.CreditEntries.Where(e => e.PaymentId == payment.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
            db.CreditEntries.RemoveRange(entries);
            invoice.Payments.Remove(payment);
            db.Payments.Remove(payment);

            InvoicePayments.Refresh(invoice);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await InvoicePayments.DetailAsync(db, invoice, DateOnly.FromDateTime(clock.GetLocalNow().DateTime), cancellationToken).ConfigureAwait(false);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>Pays an invoice, or part of it, with credit the customer already has, for example from an earlier overpayment.</summary>
internal static class ApplyCredit
{
    public sealed record Command(Guid InvoiceId, decimal Amount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<InvoiceDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var amount = Math.Round(command.Amount, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0m || amount > InvoicePayments.MaxAmount)
            {
                return Error.Validation("payment_amount_invalid", "The amount must be more than 0 and at most 10 000 000.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var invoice = await InvoicePayments.LoadAsync(db, command.InvoiceId, cancellationToken).ConfigureAwait(false);
            if (invoice is null)
            {
                return Error.NotFound("invoice_not_found", "Invoice not found.");
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                return Error.Conflict("invoice_cancelled", "A cancelled invoice cannot be paid.");
            }

            if (amount > InvoicePayments.Outstanding(invoice))
            {
                return Error.Validation("payment_exceeds_outstanding", "The invoice needs less than that. Apply at most what is left to pay.");
            }

            var balance = await InvoicePayments.CreditBalanceAsync(db, invoice.CustomerId, cancellationToken).ConfigureAwait(false);
            if (amount > balance)
            {
                return Error.Conflict("credit_insufficient", "The customer does not have that much credit.");
            }

            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            var payment = new Payment
            {
                Id = Guid.CreateVersion7(),
                InvoiceId = invoice.Id,
                Kind = PaymentKind.CreditApplied,
                Date = today,
                Amount = amount,
                CreatedAt = clock.GetUtcNow(),
            };
            invoice.Payments.Add(payment);
            db.Payments.Add(payment);
            db.CreditEntries.Add(new CreditEntry
            {
                Id = Guid.CreateVersion7(),
                CustomerId = invoice.CustomerId,
                Kind = CreditEntryKind.Applied,
                Date = today,
                Amount = -amount,
                PaymentId = payment.Id,
                Note = invoice.Number,
                CreatedAt = clock.GetUtcNow(),
            });

            InvoicePayments.Refresh(invoice);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await InvoicePayments.DetailAsync(db, invoice, today, cancellationToken).ConfigureAwait(false);
        }
    }
}

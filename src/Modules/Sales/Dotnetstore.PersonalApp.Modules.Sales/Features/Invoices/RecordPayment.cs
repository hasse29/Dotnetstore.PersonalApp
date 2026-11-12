using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>
/// Records money that arrived for an invoice. As much as the invoice still needs goes to it; anything more becomes credit on the
/// customer, which can be applied to another invoice or paid back.
/// </summary>
internal static class RecordPayment
{
    public sealed record Command(Guid InvoiceId, decimal Amount, DateOnly? Date = null, string? Note = null);

    /// <param name="Excess">How much of the payment was more than the invoice needed and became credit.</param>
    public sealed record Response(InvoiceDetailDto Invoice, decimal Excess);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<Response>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Response>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
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

            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            var date = command.Date ?? today;
            if (date < invoice.Date)
            {
                return Error.Validation("payment_date_invalid", "The payment cannot be dated before the invoice.");
            }

            var applied = Math.Min(amount, InvoicePayments.Outstanding(invoice));
            var excess = amount - applied;

            var payment = new Payment
            {
                Id = Guid.CreateVersion7(),
                InvoiceId = invoice.Id,
                Kind = PaymentKind.Received,
                Date = date,
                Amount = applied,
                Excess = excess,
                Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(),
                CreatedAt = clock.GetUtcNow(),
            };
            invoice.Payments.Add(payment);
            db.Payments.Add(payment);

            if (excess > 0m)
            {
                db.CreditEntries.Add(new CreditEntry
                {
                    Id = Guid.CreateVersion7(),
                    CustomerId = invoice.CustomerId,
                    Kind = CreditEntryKind.Overpayment,
                    Date = date,
                    Amount = excess,
                    PaymentId = payment.Id,
                    Note = invoice.Number,
                    CreatedAt = clock.GetUtcNow(),
                });
            }

            InvoicePayments.Refresh(invoice);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new Response(await InvoicePayments.DetailAsync(db, invoice, today, cancellationToken).ConfigureAwait(false), excess);
        }
    }
}

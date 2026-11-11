using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;

/// <summary>Records that credit was paid back to the customer. The money itself is paid outside the app.</summary>
internal static class RefundCredit
{
    public sealed record Command(Guid CustomerId, decimal Amount, DateOnly? Date = null, string? Note = null);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<CustomerCreditDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var amount = Math.Round(command.Amount, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0m || amount > InvoicePayments.MaxAmount)
            {
                return Error.Validation("payment_amount_invalid", "The amount must be more than 0 and at most 10 000 000.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Customers.AnyAsync(c => c.Id == command.CustomerId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("customer_not_found", "Customer not found.");
            }

            var balance = await InvoicePayments.CreditBalanceAsync(db, command.CustomerId, cancellationToken).ConfigureAwait(false);
            if (amount > balance)
            {
                return Error.Conflict("credit_insufficient", "The customer does not have that much credit.");
            }

            var date = command.Date ?? DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            db.CreditEntries.Add(new CreditEntry
            {
                Id = Guid.CreateVersion7(),
                CustomerId = command.CustomerId,
                Kind = CreditEntryKind.Refund,
                Date = date,
                Amount = -amount,
                Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(),
                CreatedAt = clock.GetUtcNow(),
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await new GetCustomerCredit.Handler(dbFactory).HandleAsync(new GetCustomerCredit.Query(command.CustomerId), cancellationToken).ConfigureAwait(false);
        }
    }
}

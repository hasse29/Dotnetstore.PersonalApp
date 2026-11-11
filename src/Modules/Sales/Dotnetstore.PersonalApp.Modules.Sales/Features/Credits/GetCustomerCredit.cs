using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;

/// <summary>A customer's credit ledger: what was added, applied and paid back, newest first.</summary>
internal static class GetCustomerCredit
{
    public sealed record Query(Guid CustomerId);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<CustomerCreditDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == query.CustomerId, cancellationToken).ConfigureAwait(false);
            if (customer is null)
            {
                return Error.NotFound("customer_not_found", "Customer not found.");
            }

            var entries = (await db.CreditEntries.AsNoTracking()
                    .Where(e => e.CustomerId == query.CustomerId)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .ToList();

            var balance = await InvoicePayments.CreditBalanceAsync(db, query.CustomerId, cancellationToken).ConfigureAwait(false);

            return new CustomerCreditDetailDto(
                new CustomerCreditDto(customer.Id, customer.Name, balance),
                [.. entries.Select(e => new CreditEntryDto(e.Id, e.Kind, e.Date, e.Amount, e.Note))]);
        }
    }
}

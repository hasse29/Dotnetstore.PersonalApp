using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>Deletes a customer that has no offers; one with offers is made inactive instead.</summary>
internal static class DeleteCustomer
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Offers.AnyAsync(o => o.CustomerId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_in_use", "The customer has offers.");
            }

            var deleted = await db.Customers
                .Where(c => c.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("customer_not_found", "Customer not found.") : command.Id;
        }
    }
}

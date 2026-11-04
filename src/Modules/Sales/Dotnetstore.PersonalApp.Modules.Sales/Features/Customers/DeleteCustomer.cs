using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal static class DeleteCustomer
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.SalesOrders.AnyAsync(x => x.CustomerId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.Customers
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("customer_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
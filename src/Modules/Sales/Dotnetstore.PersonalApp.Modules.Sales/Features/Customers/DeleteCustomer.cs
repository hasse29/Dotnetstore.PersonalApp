using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>Deletes a customer that has no offers; one with offers is made inactive instead.</summary>
internal static class DeleteCustomer
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IEventPublisher events)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Offers.AnyAsync(o => o.CustomerId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("customer_in_use", "The customer has offers.");
            }

            // The delete and the event are one unit: if a consumer in another module fails, the customer is kept and the
            // user can simply try again (the consumers' deletes are idempotent), instead of leaving orphaned contact details.
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.Customers
                .Where(c => c.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("customer_not_found", "Customer not found.");
            }

            await events.PublishAsync(new CustomerDeleted(command.Id), cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return command.Id;
        }
    }
}

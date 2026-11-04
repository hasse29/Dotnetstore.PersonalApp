using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal static class DeleteSalesPerson
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Stores.AnyAsync(x => x.SalesPersonId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_person_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.SalesOrders.AnyAsync(x => x.SalesPersonId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_person_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.SalesPeople
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("sales_person_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;

internal static class DeleteSalesTerritory
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.SalesPeople.AnyAsync(x => x.TerritoryId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_territory_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.SalesTerritoryHistories.AnyAsync(x => x.TerritoryId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_territory_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.Customers.AnyAsync(x => x.TerritoryId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_territory_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.SalesOrders.AnyAsync(x => x.TerritoryId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_territory_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.SalesTerritories
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("sales_territory_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
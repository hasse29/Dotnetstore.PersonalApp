using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;

internal static class DeleteShipMethod
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.PurchaseOrders.AnyAsync(x => x.ShipMethodId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("ship_method_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.ShipMethods
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("ship_method_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
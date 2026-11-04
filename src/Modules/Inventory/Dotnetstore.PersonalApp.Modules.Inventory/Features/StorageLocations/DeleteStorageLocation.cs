using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

internal static class DeleteStorageLocation
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.StockMovements.AnyAsync(x => x.LocationId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("storage_location_in_use", "The record is in use and cannot be deleted.");
            }

            if (await db.InventoryCountLines.AnyAsync(x => x.LocationId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("storage_location_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.StorageLocations
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("storage_location_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
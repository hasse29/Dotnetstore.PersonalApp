using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

/// <summary>A location that has ever held stock, or that a count refers to, cannot be deleted: make it inactive instead.</summary>
internal static class DeleteStorageLocation
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var inUse =
                await db.StockMovements.AnyAsync(m => m.LocationId == command.Id, cancellationToken).ConfigureAwait(false) ||
                await db.StockLevels.AnyAsync(l => l.LocationId == command.Id, cancellationToken).ConfigureAwait(false) ||
                await db.InventoryCounts.AnyAsync(c => c.LocationId == command.Id, cancellationToken).ConfigureAwait(false) ||
                await db.InventoryCountLines.AnyAsync(l => l.LocationId == command.Id, cancellationToken).ConfigureAwait(false);
            if (inUse)
            {
                return Error.Conflict("storage_location_in_use", "The storage location has stock history.");
            }

            var deleted = await db.StorageLocations
                .Where(l => l.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("storage_location_not_found", "Storage location not found.") : command.Id;
        }
    }
}

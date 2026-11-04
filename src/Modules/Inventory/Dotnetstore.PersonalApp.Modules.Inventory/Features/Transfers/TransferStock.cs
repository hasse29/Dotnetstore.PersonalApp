using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Transfers;

/// <summary>Moves stock of a product from one location to another as a movement out and a movement in.</summary>
internal static class TransferStock
{
    public sealed record Command(Guid ProductId, Guid FromLocationId, Guid ToLocationId, decimal Quantity, DateOnly Date, string? Reference);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, ExternalReferences references)
    {
        /// <returns>What is left of the product at the location it was moved from.</returns>
        public async Task<Result<decimal>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.Quantity <= 0m)
            {
                return Error.Validation("stock_transfer_quantity", "The quantity must be more than zero.");
            }

            if (command.FromLocationId == command.ToLocationId)
            {
                return Error.Validation("stock_transfer_same_location", "Choose two different locations.");
            }

            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("stock_transfer_not_found", "The product or a location was not found.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var locations = await db.StorageLocations.AsNoTracking()
                .CountAsync(x => x.Id == command.FromLocationId || x.Id == command.ToLocationId, cancellationToken)
                .ConfigureAwait(false);
            if (locations != 2)
            {
                return Error.NotFound("stock_transfer_not_found", "The product or a location was not found.");
            }

            var available = await StockLedger.QuantityAsync(db, command.ProductId, command.FromLocationId, null, cancellationToken).ConfigureAwait(false);
            if (available < command.Quantity)
            {
                return Error.Conflict("stock_movement_insufficient_stock", "There is not enough in stock at that location.");
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            db.StockMovements.Add(Movement(command, command.FromLocationId, "TransferOut"));
            db.StockMovements.Add(Movement(command, command.ToLocationId, "TransferIn"));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await StockLedger.RecalculateAsync(db, command.ProductId, command.FromLocationId, cancellationToken).ConfigureAwait(false);
            await StockLedger.RecalculateAsync(db, command.ProductId, command.ToLocationId, cancellationToken).ConfigureAwait(false);
            if (await StockLedger.GuardAsync(db, command.ProductId, command.FromLocationId, cancellationToken).ConfigureAwait(false) is { } guardError)
            {
                return guardError;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return available - command.Quantity;
        }

        private static StockMovement Movement(Command command, Guid locationId, string type) => new()
        {
            Id = Guid.CreateVersion7(),
            Date = command.Date,
            ProductId = command.ProductId,
            LocationId = locationId,
            Type = type,
            Quantity = command.Quantity,
            Reference = string.IsNullOrWhiteSpace(command.Reference) ? null : command.Reference.Trim(),
        };
    }
}

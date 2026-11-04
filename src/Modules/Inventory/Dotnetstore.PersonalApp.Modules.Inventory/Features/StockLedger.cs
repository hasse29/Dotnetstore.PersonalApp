using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features;

/// <summary>
/// The stock movements are the ledger and a stock level is what they add up to. Every movement that is
/// saved, changed or deleted brings the level of its product and location back in step.
/// </summary>
internal static class StockLedger
{
    private static readonly HashSet<string> Outgoing = ["Issue", "Scrap", "TransferOut", "AdjustmentDown"];

    public static bool IsOutgoing(string type) => Outgoing.Contains(type);

    /// <summary>The effect of a movement on the stock: positive for goods coming in, negative for goods going out.</summary>
    public static decimal Signed(string type, decimal quantity) => IsOutgoing(type) ? -quantity : quantity;

    public static async Task RecalculateAsync(InventoryDbContext db, Guid productId, Guid locationId, CancellationToken cancellationToken)
    {
        var quantity = await QuantityAsync(db, productId, locationId, null, cancellationToken).ConfigureAwait(false);

        var level = await db.StockLevels
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.LocationId == locationId, cancellationToken)
            .ConfigureAwait(false);
        if (level is null)
        {
            level = new StockLevel { Id = Guid.CreateVersion7(), ProductId = productId, LocationId = locationId };
            db.StockLevels.Add(level);
        }

        level.Quantity = quantity;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stops a movement out of stock that does not exist; called before a movement is saved.</summary>
    public static async Task<Error?> GuardAsync(InventoryDbContext db, StockMovement movement, CancellationToken cancellationToken)
    {
        if (!IsOutgoing(movement.Type))
        {
            return null;
        }

        var available = await QuantityAsync(db, movement.ProductId, movement.LocationId, movement.Id, cancellationToken).ConfigureAwait(false);

        return available < movement.Quantity
            ? Error.Conflict("stock_movement_insufficient_stock", "There is not enough in stock at that location.")
            : null;
    }

    /// <summary>What the movements of a product at a location add up to, leaving one movement out (the one being saved).</summary>
    public static async Task<decimal> QuantityAsync(InventoryDbContext db, Guid productId, Guid locationId, Guid? except, CancellationToken cancellationToken)
    {
        // SQLite cannot sum decimals, so the movements are added up here.
        var movements = await db.StockMovements.AsNoTracking()
            .Where(x => x.ProductId == productId && x.LocationId == locationId && x.Id != except)
            .Select(x => new { x.Type, x.Quantity })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return movements.Sum(x => Signed(x.Type, x.Quantity));
    }
}

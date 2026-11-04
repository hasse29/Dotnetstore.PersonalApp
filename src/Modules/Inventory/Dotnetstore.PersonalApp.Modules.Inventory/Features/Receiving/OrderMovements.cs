using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;

/// <summary>Receives goods against a purchase order or issues them against a sales order; each is a stock movement that refers to the order number.</summary>
internal static class OrderMovements
{
    public sealed record Line(Guid LineId, Guid ProductId, decimal Ordered, decimal Quantity);

    public sealed record Command(bool Receiving, Guid LocationId, string OrderNumber, DateOnly Date, IReadOnlyList<Line> Lines);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IEventPublisher events, ExternalReferences references)
    {
        /// <summary>How much of each product has already been received or issued against an order.</summary>
        public async Task<IReadOnlyDictionary<Guid, decimal>> HandledAsync(string orderNumber, bool receiving, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = receiving ? "Receipt" : "Issue";
            var movements = await db.StockMovements.AsNoTracking()
                .Where(x => x.Reference == orderNumber && x.Type == type)
                .Select(x => new { x.ProductId, x.Quantity })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return movements.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        }

        /// <returns>How many lines were received or issued.</returns>
        public async Task<Result<int>> RecordAsync(Command command, CancellationToken cancellationToken)
        {
            var lines = command.Lines.Where(x => x.Quantity > 0m).ToList();
            if (lines.Count == 0)
            {
                return Error.Validation("goods_movement_nothing", "Enter a quantity for at least one line.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.StorageLocations.AnyAsync(x => x.Id == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("goods_movement_not_found", "The location or a product was not found.");
            }

            foreach (var productId in lines.Select(x => x.ProductId).Distinct())
            {
                if (!await references.ProductExistsAsync(productId, cancellationToken).ConfigureAwait(false))
                {
                    return Error.NotFound("goods_movement_not_found", "The location or a product was not found.");
                }
            }

            // All lines are checked before anything is saved, so an issue never goes through half way.
            if (!command.Receiving)
            {
                foreach (var group in lines.GroupBy(x => x.ProductId))
                {
                    var available = await StockLedger.QuantityAsync(db, group.Key, command.LocationId, null, cancellationToken).ConfigureAwait(false);
                    if (available < group.Sum(x => x.Quantity))
                    {
                        return Error.Conflict("stock_movement_insufficient_stock", "There is not enough in stock at that location.");
                    }
                }
            }

            // Nothing may go past what the order asks for, which also stops a repeated submit from booking the same goods twice.
            var handled = await HandledAsync(command.OrderNumber, command.Receiving, cancellationToken).ConfigureAwait(false);
            foreach (var group in command.Lines.GroupBy(x => x.ProductId))
            {
                if (handled.GetValueOrDefault(group.Key) + group.Sum(x => x.Quantity) > group.Sum(x => x.Ordered))
                {
                    return Error.Conflict("goods_movement_exceeds_order", "The quantity is more than the order has outstanding.");
                }
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            foreach (var line in lines)
            {
                db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.CreateVersion7(),
                    Date = command.Date,
                    ProductId = line.ProductId,
                    LocationId = command.LocationId,
                    Type = command.Receiving ? "Receipt" : "Issue",
                    Quantity = line.Quantity,
                    Reference = command.OrderNumber,
                });
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            foreach (var productId in lines.Select(x => x.ProductId).Distinct())
            {
                await StockLedger.RecalculateAsync(db, productId, command.LocationId, cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            if (command.Receiving)
            {
                foreach (var line in lines)
                {
                    await events.PublishAsync(new GoodsReceived(line.LineId, line.Quantity), cancellationToken).ConfigureAwait(false);
                }
            }

            return lines.Count;
        }
    }
}

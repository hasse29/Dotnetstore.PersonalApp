using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

/// <summary>Brings the stock to what was counted: each counted line that differs gets an adjustment movement, then the count is closed.</summary>
internal static class PostInventoryCount
{
    public sealed record Command(Guid InventoryCountId);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        /// <returns>How many adjustments were made.</returns>
        public async Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var count = await db.InventoryCounts.FirstOrDefaultAsync(x => x.Id == command.InventoryCountId, cancellationToken).ConfigureAwait(false);
            if (count is null)
            {
                return Error.NotFound("inventory_count_not_found", "The record was not found.");
            }

            if (count.Posted)
            {
                return Error.Conflict("inventory_count_posted", "The count has been posted and cannot be changed.");
            }

            var lines = await db.InventoryCountLines.AsNoTracking()
                .Where(x => x.InventoryCountId == count.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var adjusted = 0;
            foreach (var line in lines.Where(x => x.CountedQty is not null))
            {
                // The stock may have moved since the line was made, so the count is compared with what is there now.
                var current = await StockLedger.QuantityAsync(db, line.ProductId, line.LocationId, null, cancellationToken).ConfigureAwait(false);
                var difference = line.CountedQty!.Value - current;
                if (difference == 0m)
                {
                    continue;
                }

                db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.CreateVersion7(),
                    Date = count.Date,
                    ProductId = line.ProductId,
                    LocationId = line.LocationId,
                    Type = difference > 0m ? "AdjustmentUp" : "AdjustmentDown",
                    Quantity = Math.Abs(difference),
                    Reference = count.Number,
                });
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await StockLedger.RecalculateAsync(db, line.ProductId, line.LocationId, cancellationToken).ConfigureAwait(false);
                adjusted++;
            }

            count.Posted = true;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return adjusted;
        }
    }
}

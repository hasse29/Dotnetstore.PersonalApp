using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

/// <summary>Adds a line for every product at every location of the warehouse that holds stock, with today's quantity as the expected one.</summary>
internal static class FillInventoryCount
{
    public sealed record Command(Guid InventoryCountId);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        /// <returns>How many lines were added; lines that already exist are left alone.</returns>
        public async Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var count = await db.InventoryCounts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.InventoryCountId, cancellationToken)
                .ConfigureAwait(false);
            if (count is null)
            {
                return Error.NotFound("inventory_count_not_found", "The record was not found.");
            }

            if (count.Posted)
            {
                return Error.Conflict("inventory_count_posted", "The count has been posted and cannot be changed.");
            }

            var levels = await db.StockLevels.AsNoTracking()
                .Where(x => x.Location.WarehouseId == count.WarehouseId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var existing = await db.InventoryCountLines
                .Where(x => x.InventoryCountId == count.Id)
                .Select(x => new { x.ProductId, x.LocationId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var known = existing.Select(x => (x.ProductId, x.LocationId)).ToHashSet();

            var added = 0;
            foreach (var level in levels.Where(x => x.Quantity != 0m && !known.Contains((x.ProductId, x.LocationId))))
            {
                db.InventoryCountLines.Add(new InventoryCountLine
                {
                    Id = Guid.CreateVersion7(),
                    InventoryCountId = count.Id,
                    ProductId = level.ProductId,
                    LocationId = level.LocationId,
                    ExpectedQty = level.Quantity,
                });
                added++;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return added;
        }
    }
}

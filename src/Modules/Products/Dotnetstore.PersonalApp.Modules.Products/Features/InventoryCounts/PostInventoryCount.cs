using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>
/// Makes the stock match what was counted. Each counted line is compared with the stock as it is now (it may have moved since the
/// count was started) and the difference is posted as a <see cref="StockMovementType.CountAdjustment"/>. Uncounted lines are left alone.
/// A posted count is final.
/// </summary>
internal static class PostInventoryCount
{
    public sealed record Command(Guid Id);

    /// <returns>How many adjustments were made.</returns>
    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<int>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var count = await db.InventoryCounts.Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (count is null)
            {
                return Error.NotFound("inventory_count_not_found", "Inventory count not found.");
            }

            if (count.PostedAt is not null)
            {
                return Error.Conflict("inventory_count_posted", "The inventory count has already been posted.");
            }

            var now = clock.GetUtcNow();
            var adjustments = 0;

            foreach (var line in count.Lines.Where(l => l.CountedQuantity is not null))
            {
                var current = await db.StockLevels.AsNoTracking()
                    .Where(l => l.ProductId == line.ProductId && l.LocationId == line.LocationId)
                    .Select(l => (decimal?)l.Quantity)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false) ?? 0m;

                var difference = line.CountedQuantity!.Value - current;
                line.ExpectedQuantity = current;

                if (difference == 0m)
                {
                    continue;
                }

                var applied = await StockLedger.ApplyAsync(
                    db, line.ProductId, line.LocationId, difference, StockMovementType.CountAdjustment, now, count.Name,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!applied.IsSuccess)
                {
                    return applied.Error;
                }

                adjustments++;
            }

            count.PostedAt = now;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return adjustments;
        }
    }
}

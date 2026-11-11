using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Stock;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>What other modules use to take goods out of stock and put them back. Each call is one save, so it is all or nothing.</summary>
internal sealed class StockService(IDbContextFactory<ProductsDbContext> dbFactory, TimeProvider clock) : IStockService
{
    public async Task<Result<IReadOnlyList<StockAllocation>>> IssueAsync(Guid productId, decimal quantity, string note, CancellationToken cancellationToken)
    {
        if (quantity <= 0m || quantity > StockLedger.MaxQuantity)
        {
            return Error.Validation("stock_quantity_invalid", "Quantity must be more than 0 and at most 1 000 000.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        if (await StockLedger.CheckProductAsync(db, productId, cancellationToken).ConfigureAwait(false) is { } productError)
        {
            return productError;
        }

        // Sorted in memory: SQLite cannot order by a decimal column.
        var levels = (await db.StockLevels.AsNoTracking()
                .Where(l => l.ProductId == productId && l.Quantity > 0)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .OrderByDescending(l => l.Quantity)
            .ThenBy(l => l.Id)
            .ToList();

        var allocations = new List<StockAllocation>();
        var remaining = quantity;
        foreach (var level in levels)
        {
            if (remaining <= 0m)
            {
                break;
            }

            var take = Math.Min(level.Quantity, remaining);
            allocations.Add(new StockAllocation(level.LocationId, take));
            remaining -= take;
        }

        if (remaining > 0m)
        {
            return Error.Conflict("stock_insufficient_total", "There is not enough in stock.");
        }

        var cleanNote = StockLedger.NormalizeNote(note);
        foreach (var allocation in allocations)
        {
            var applied = await StockLedger.ApplyAsync(
                db, productId, allocation.LocationId, -allocation.Quantity, StockMovementType.Issue, clock.GetUtcNow(),
                cleanNote, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied.IsSuccess)
            {
                return applied.Error;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return allocations;
    }

    public async Task<Result<bool>> ReturnAsync(Guid productId, IReadOnlyList<StockAllocation> allocations, string note, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var cleanNote = StockLedger.NormalizeNote(note);
        foreach (var allocation in allocations.Where(a => a.Quantity > 0m))
        {
            // The location may have been made inactive since, which does not stop goods from being put back.
            if (await StockLedger.CheckLocationAsync(db, allocation.LocationId, requireActive: false, cancellationToken).ConfigureAwait(false) is { } locationError)
            {
                return locationError;
            }

            var applied = await StockLedger.ApplyAsync(
                db, productId, allocation.LocationId, allocation.Quantity, StockMovementType.Receipt, clock.GetUtcNow(),
                cleanNote, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied.IsSuccess)
            {
                return applied.Error;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

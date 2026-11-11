using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Stock;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

/// <summary>
/// Keeps the stock in step with what has been picked. Picking more takes goods out of stock; picking less, or cancelling, puts
/// them back where they came from. Stock and Sales are separate databases, so each line is saved right after its stock change:
/// a failure part-way leaves every finished line consistent.
/// </summary>
internal static class PickStock
{
    /// <summary>Moves the picked quantity of the line to <paramref name="target"/>, changing the stock by the difference. The caller saves.</summary>
    /// <returns>The reason it failed, or null; on a failure the line is unchanged.</returns>
    public static async Task<Error?> SetPickedAsync(
        SalesDbContext db,
        IStockService stock,
        PickListLine line,
        decimal target,
        string note,
        CancellationToken cancellationToken)
    {
        var delta = target - line.PickedQuantity;
        if (delta == 0m)
        {
            return null;
        }

        // A line that did not come from a product has nothing in stock.
        if (line.ProductId is not { } productId)
        {
            line.PickedQuantity = target;
            return null;
        }

        if (delta > 0m)
        {
            var issued = await stock.IssueAsync(productId, delta, note, cancellationToken).ConfigureAwait(false);
            if (!issued.IsSuccess)
            {
                return issued.Error;
            }

            foreach (var allocation in issued.Value)
            {
                var existing = line.Allocations.FirstOrDefault(a => a.LocationId == allocation.LocationId);
                if (existing is null)
                {
                    var created = new PickAllocation { Id = Guid.CreateVersion7(), PickListLineId = line.Id, LocationId = allocation.LocationId, Quantity = allocation.Quantity };
                    line.Allocations.Add(created);
                    db.PickAllocations.Add(created);
                }
                else
                {
                    existing.Quantity += allocation.Quantity;
                }
            }
        }
        else
        {
            var error = await ReturnAsync(db, stock, line, -delta, note, cancellationToken).ConfigureAwait(false);
            if (error is not null)
            {
                return error;
            }
        }

        line.PickedQuantity = target;
        return null;
    }

    /// <summary>Puts <paramref name="quantity"/> of the line back in stock, from the most recent pick first.</summary>
    private static async Task<Error?> ReturnAsync(
        SalesDbContext db,
        IStockService stock,
        PickListLine line,
        decimal quantity,
        string note,
        CancellationToken cancellationToken)
    {
        var back = new List<StockAllocation>();
        var remaining = quantity;
        foreach (var allocation in line.Allocations.OrderByDescending(a => a.Id))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var take = Math.Min(allocation.Quantity, remaining);
            back.Add(new StockAllocation(allocation.LocationId, take));
            remaining -= take;
        }

        var returned = await stock.ReturnAsync(line.ProductId!.Value, back, note, cancellationToken).ConfigureAwait(false);
        if (!returned.IsSuccess)
        {
            return returned.Error;
        }

        foreach (var part in back)
        {
            var allocation = line.Allocations.First(a => a.LocationId == part.LocationId);
            allocation.Quantity -= part.Quantity;
            if (allocation.Quantity <= 0m)
            {
                line.Allocations.Remove(allocation);
                db.PickAllocations.Remove(allocation);
            }
        }

        return null;
    }
}

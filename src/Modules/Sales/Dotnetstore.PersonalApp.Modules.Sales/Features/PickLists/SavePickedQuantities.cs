using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Stock;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

/// <summary>
/// Saves how much of each line has been picked and takes that out of stock (or puts it back, if less is picked than before).
/// When every line is fully picked the list is complete and its order ready to invoice; picking less again reopens both, until
/// the order has been invoiced. If the stock runs out part-way, the lines before it stay saved and the error is returned.
/// </summary>
internal static class SavePickedQuantities
{
    public const decimal MaxQuantity = 1_000_000m;

    public sealed record Entry(Guid LineId, decimal PickedQuantity);

    public sealed record Command(Guid PickListId, IReadOnlyList<Entry> Entries);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IStockService stock)
    {
        public async Task<Result<PickListDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var pickList = await db.PickLists
                .Include(p => p.Lines).ThenInclude(l => l.Allocations)
                .FirstOrDefaultAsync(p => p.Id == command.PickListId, cancellationToken)
                .ConfigureAwait(false);
            if (pickList is null)
            {
                return Error.NotFound("picklist_not_found", "Pick list not found.");
            }

            if (pickList.Status == PickListStatus.Cancelled)
            {
                return Error.Conflict("picklist_cancelled", "The pick list is cancelled.");
            }

            var order = await db.Orders.FirstAsync(o => o.Id == pickList.OrderId, cancellationToken).ConfigureAwait(false);
            if (order.Status == OrderStatus.Invoiced)
            {
                return Error.Conflict("picklist_locked", "The order has been invoiced, so the picking cannot be changed.");
            }

            var targets = new List<(PickListLine Line, decimal Quantity)>();
            foreach (var entry in command.Entries)
            {
                var line = pickList.Lines.FirstOrDefault(l => l.Id == entry.LineId);
                if (line is null)
                {
                    return Error.NotFound("picklist_line_not_found", "Pick list line not found.");
                }

                // More than ordered is rejected: surplus would be invoiced by mistake.
                if (entry.PickedQuantity < 0 || entry.PickedQuantity > MaxQuantity || entry.PickedQuantity > line.Quantity)
                {
                    return Error.Validation("picklist_picked_invalid", "The picked quantity must be between 0 and the ordered quantity.");
                }

                targets.Add((line, Math.Round(entry.PickedQuantity, 2, MidpointRounding.AwayFromZero)));
            }

            Error? failure = null;
            foreach (var (line, quantity) in targets)
            {
                failure = await PickStock.SetPickedAsync(db, stock, line, quantity, $"Pick list {pickList.Number}", cancellationToken).ConfigureAwait(false);
                if (failure is not null)
                {
                    break;
                }

                // Saved line by line: the stock change of a finished line must not be lost if a later line fails.
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var complete = pickList.Lines.All(l => l.PickedQuantity >= l.Quantity);
            pickList.Status = complete ? PickListStatus.Complete : PickListStatus.Open;
            order.Status = complete ? OrderStatus.ReadyToInvoice : OrderStatus.Open;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (failure is not null)
            {
                return failure;
            }

            return PickListDetailDto.From(pickList, order);
        }
    }
}

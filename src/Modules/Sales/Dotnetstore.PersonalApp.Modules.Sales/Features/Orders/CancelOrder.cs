using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Stock;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

/// <summary>
/// Cancels an order that has not been invoiced, together with its pick list, and puts everything picked back in stock. The offer
/// stays accepted and can be made into a new order.
/// </summary>
internal static class CancelOrder
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IStockService stock)
    {
        public Task<Result<OrderDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<OrderDetailDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var order = await db.Orders
                .Include(o => o.Lines)
                .Include(o => o.PickList!).ThenInclude(p => p.Lines).ThenInclude(l => l.Allocations)
                .FirstOrDefaultAsync(o => o.Id == command.Id, cancellationToken)
                .ConfigureAwait(false);

            if (order is null)
            {
                return Error.NotFound("order_not_found", "Order not found.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return Error.Conflict("order_already_cancelled", "The order is already cancelled.");
            }

            if (order.Status == OrderStatus.Invoiced)
            {
                return Error.Conflict("order_invoiced", "The order has been invoiced. Cancel the invoice first.");
            }

            if (order.PickList is { } pickList)
            {
                foreach (var line in pickList.Lines)
                {
                    // Saved line by line, so a line that is back in stock is never put back twice if a later one fails.
                    var error = await PickStock.SetPickedAsync(db, stock, line, 0m, $"Cancelled {pickList.Number}", cancellationToken).ConfigureAwait(false);
                    if (error is not null)
                    {
                        return error;
                    }

                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                pickList.Status = PickListStatus.Cancelled;
            }

            order.Status = OrderStatus.Cancelled;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return OrderDetailDto.From(order);
        }
    }
}

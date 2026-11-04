using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

internal static class DeleteStockMovement
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);



            var keys = await db.StockMovements
                .Where(x => x.Id == command.Id)
                .Select(x => new { x.ProductId, x.LocationId })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var deleted = await db.StockMovements
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("stock_movement_not_found", "The record was not found.");
            }

            if (keys is not null)
            {
                await StockLedger.RecalculateAsync(db, keys.ProductId, keys.LocationId, cancellationToken).ConfigureAwait(false);
            }

            return command.Id;
        }
    }
}
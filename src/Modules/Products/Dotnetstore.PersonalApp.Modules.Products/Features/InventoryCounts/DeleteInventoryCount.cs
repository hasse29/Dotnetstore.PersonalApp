using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>Deletes a count and its lines. Adjustments already posted stay in the stock history.</summary>
internal static class DeleteInventoryCount
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.InventoryCounts
                .Where(c => c.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("inventory_count_not_found", "Inventory count not found.") : command.Id;
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <summary>Stores the quantities counted so far; a null quantity means "not counted yet". Only open counts can be changed.</summary>
internal static class SaveInventoryCountLines
{
    public sealed record Entry(Guid LineId, decimal? CountedQuantity);

    public sealed record Command(Guid CountId, IReadOnlyList<Entry> Entries);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.Entries.Any(e => e.CountedQuantity is < 0m or > StockLedger.MaxQuantity))
            {
                return Error.Validation("stock_counted_invalid", "A counted quantity must be between 0 and 1 000 000.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var count = await db.InventoryCounts.Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == command.CountId, cancellationToken).ConfigureAwait(false);
            if (count is null)
            {
                return Error.NotFound("inventory_count_not_found", "Inventory count not found.");
            }

            if (count.PostedAt is not null)
            {
                return Error.Conflict("inventory_count_posted", "The inventory count has been posted.");
            }

            var lines = count.Lines.ToDictionary(l => l.Id);
            foreach (var entry in command.Entries)
            {
                if (lines.TryGetValue(entry.LineId, out var line))
                {
                    line.CountedQuantity = entry.CountedQuantity is { } counted ? Math.Round(counted, 2, MidpointRounding.AwayFromZero) : null;
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return command.CountId;
        }
    }
}

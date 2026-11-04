using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

/// <summary>Every inventory count as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListInventoryCountOptions
{
    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.InventoryCounts.AsNoTracking()
                .OrderBy(t => t.Number)
                .Select(t => new OptionDto(t.Id, t.Number))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
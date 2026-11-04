using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

/// <summary>Every storage location as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListStorageLocationOptions
{
    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.StorageLocations.AsNoTracking()
                .OrderBy(t => t.Code)
                .Select(t => new OptionDto(t.Id, t.Code))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
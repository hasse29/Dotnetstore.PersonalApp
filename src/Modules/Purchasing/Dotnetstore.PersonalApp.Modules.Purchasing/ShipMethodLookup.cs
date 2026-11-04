using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Dotnetstore.PersonalApp.Shared.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing;

internal sealed class ShipMethodLookup(IDbContextFactory<PurchasingDbContext> dbFactory) : IShipMethodLookup
{
    public async Task<IReadOnlyList<ReferenceItem>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.ShipMethods.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ReferenceItem(x.Id, x.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.ShipMethods.AnyAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
    }
}
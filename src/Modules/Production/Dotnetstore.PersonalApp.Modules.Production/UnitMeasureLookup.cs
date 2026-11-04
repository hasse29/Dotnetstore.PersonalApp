using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Lookups;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production;

internal sealed class UnitMeasureLookup(IDbContextFactory<ProductionDbContext> dbFactory) : IUnitMeasureLookup
{
    public async Task<IReadOnlyList<ReferenceItem>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.UnitMeasures.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ReferenceItem(x.Id, x.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.UnitMeasures.AnyAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
    }
}
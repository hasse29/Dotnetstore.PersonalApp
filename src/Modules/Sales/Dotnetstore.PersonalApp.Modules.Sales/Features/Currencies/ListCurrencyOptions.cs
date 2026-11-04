using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;

/// <summary>Every currency as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListCurrencyOptions
{
    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.Currencies.AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new OptionDto(t.Id, t.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
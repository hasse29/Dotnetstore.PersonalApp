using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;

/// <summary>Every special offer as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListSpecialOfferOptions
{
    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.SpecialOffers.AsNoTracking()
                .OrderBy(t => t.Description)
                .Select(t => new OptionDto(t.Id, t.Description))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

/// <summary>Every scrap reason as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListScrapReasonOptions
{
    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.ScrapReasons.AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new OptionDto(t.Id, t.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
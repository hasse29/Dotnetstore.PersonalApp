using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

/// <summary>Every work order as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListWorkOrderOptions
{
    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.WorkOrders.AsNoTracking()
                .OrderBy(t => t.Number)
                .Select(t => new OptionDto(t.Id, t.Number))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
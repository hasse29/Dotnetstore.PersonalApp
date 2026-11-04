using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

/// <summary>Every purchase order as a drop-down entry, for the editors of the entities that point at it.</summary>
internal static class ListPurchaseOrderOptions
{
    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<OptionDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.PurchaseOrders.AsNoTracking()
                .OrderBy(t => t.Number)
                .Select(t => new OptionDto(t.Id, t.Number))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
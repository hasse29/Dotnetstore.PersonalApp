using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

internal static class GetPickList
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<PickListDetailDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var pickList = await db.PickLists.AsNoTracking().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken).ConfigureAwait(false);
            if (pickList is null)
            {
                return Error.NotFound("picklist_not_found", "Pick list not found.");
            }

            var order = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == pickList.OrderId, cancellationToken).ConfigureAwait(false);

            return PickListDetailDto.From(pickList, order);
        }
    }
}

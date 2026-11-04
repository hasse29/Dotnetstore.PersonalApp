using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;

internal static class DeletePurchaseOrderDetail
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);



            var parentId = await db.PurchaseOrderDetails
                .Where(x => x.Id == command.Id)
                .Select(x => (Guid?)x.PurchaseOrderId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var deleted = await db.PurchaseOrderDetails
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("purchase_order_detail_not_found", "The record was not found.");
            }

            if (parentId is { } parent)
            {
                await OrderTotals.RecalculatePurchaseOrderAsync(db, parent, cancellationToken).ConfigureAwait(false);
            }

            return command.Id;
        }
    }
}
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;

internal static class DeleteSalesOrderDetail
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);



            var parentId = await db.SalesOrderDetails
                .Where(x => x.Id == command.Id)
                .Select(x => (Guid?)x.SalesOrderId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var deleted = await db.SalesOrderDetails
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("sales_order_detail_not_found", "The record was not found.");
            }

            if (parentId is { } parent)
            {
                await OrderTotals.RecalculateSalesOrderAsync(db, parent, cancellationToken).ConfigureAwait(false);
            }

            return command.Id;
        }
    }
}
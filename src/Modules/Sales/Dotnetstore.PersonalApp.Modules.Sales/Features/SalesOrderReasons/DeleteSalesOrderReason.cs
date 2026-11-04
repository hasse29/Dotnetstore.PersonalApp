using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;

internal static class DeleteSalesOrderReason
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);



            var deleted = await db.SalesOrderReasons
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("sales_order_reason_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
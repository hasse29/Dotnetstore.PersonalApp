using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;

internal static class DeleteSalesReason
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.SalesOrderReasons.AnyAsync(x => x.SalesReasonId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_reason_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.SalesReasons
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("sales_reason_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
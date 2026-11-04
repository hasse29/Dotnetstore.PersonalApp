using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

internal static class DeleteScrapReason
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.WorkOrders.AnyAsync(x => x.ScrapReasonId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("scrap_reason_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.ScrapReasons
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("scrap_reason_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
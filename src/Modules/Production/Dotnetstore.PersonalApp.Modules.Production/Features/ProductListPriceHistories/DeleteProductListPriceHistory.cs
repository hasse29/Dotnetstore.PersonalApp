using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductListPriceHistories;

internal static class DeleteProductListPriceHistory
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);



            var deleted = await db.ProductListPriceHistories
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("product_list_price_history_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
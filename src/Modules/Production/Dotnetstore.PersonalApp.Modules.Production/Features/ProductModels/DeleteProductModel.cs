using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;

internal static class DeleteProductModel
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Products.AnyAsync(x => x.ModelId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_model_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.ProductModels
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("product_model_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
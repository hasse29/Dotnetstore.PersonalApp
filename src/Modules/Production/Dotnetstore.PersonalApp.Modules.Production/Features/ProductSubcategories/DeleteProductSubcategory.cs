using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;

internal static class DeleteProductSubcategory
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Products.AnyAsync(x => x.SubcategoryId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_subcategory_in_use", "The record is in use and cannot be deleted.");
            }

            var deleted = await db.ProductSubcategories
                .Where(x => x.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deleted == 0)
            {
                return Error.NotFound("product_subcategory_not_found", "The record was not found.");
            }

            return command.Id;
        }
    }
}
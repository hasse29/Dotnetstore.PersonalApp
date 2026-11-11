using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;

internal static class DeleteProductCategory
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Products.AnyAsync(p => p.CategoryId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_category_in_use", "The product category is used by existing products.");
            }

            var deleted = await db.ProductCategories
                .Where(t => t.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("product_category_not_found", "Work time type not found.") : command.Id;
        }
    }
}

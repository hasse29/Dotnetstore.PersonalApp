using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;

internal static class GetProductCategory
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Result<ProductCategoryDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<ProductCategoryDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.ProductCategories.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return type is null ? Error.NotFound("product_category_not_found", "Work time type not found.") : ProductCategoryDto.From(type);
        }
    }
}

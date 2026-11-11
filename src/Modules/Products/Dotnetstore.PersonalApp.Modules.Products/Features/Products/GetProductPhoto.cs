using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>Reads only the photo, so listing products never drags their pictures along.</summary>
internal static class GetProductPhoto
{
    public sealed record Query(Guid ProductId);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Result<byte[]?>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await db.Products.AsNoTracking()
                .Where(x => x.Id == query.ProductId)
                .Select(x => new { x.Photo })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return found is null ? Error.NotFound("product_not_found", "Product not found.") : Result<byte[]?>.Success(found.Photo);
        }
    }
}

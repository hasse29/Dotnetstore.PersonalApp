using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Photos;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>Sets or, with a null photo, removes the product's picture. Only PNG and JPEG up to 6 MB are accepted.</summary>
internal static class SetProductPhoto
{
    public sealed record Command(Guid ProductId, byte[]? Photo);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            if (PhotoRules.Validate(command.Photo) is { } invalid)
            {
                return invalid;
            }

            byte[]? thumbnail = null;
            if (command.Photo is { } photo)
            {
                // A file with a valid signature that still cannot be decoded is not a usable picture.
                thumbnail = PhotoThumbnail.Create(photo);
                if (thumbnail is null)
                {
                    return Error.Validation("photo_invalid", "The photo must be a PNG or JPEG image.");
                }
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var updated = await db.Products
                .Where(x => x.Id == command.ProductId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Photo, command.Photo).SetProperty(x => x.PhotoThumbnail, thumbnail), cancellationToken)
                .ConfigureAwait(false);

            return updated == 0 ? Error.NotFound("product_not_found", "Product not found.") : command.ProductId;
        }
    }
}

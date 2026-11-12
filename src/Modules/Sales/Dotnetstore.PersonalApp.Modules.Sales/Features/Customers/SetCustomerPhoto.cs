using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Photos;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>Sets or, with a null photo, removes the customer's picture. Only PNG and JPEG up to 6 MB are accepted.</summary>
internal static class SetCustomerPhoto
{
    public sealed record Command(Guid CustomerId, byte[]? Photo);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
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

            var updated = await db.Customers
                .Where(x => x.Id == command.CustomerId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Photo, command.Photo).SetProperty(x => x.PhotoThumbnail, thumbnail), cancellationToken)
                .ConfigureAwait(false);

            return updated == 0 ? Error.NotFound("customer_not_found", "Customer not found.") : command.CustomerId;
        }
    }
}

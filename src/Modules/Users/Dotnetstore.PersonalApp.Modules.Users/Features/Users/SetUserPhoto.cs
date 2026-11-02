using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Dotnetstore.PersonalApp.Shared.Photos;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

/// <summary>Sets or, with a null photo, removes the user's picture. Only PNG and JPEG up to 6 MB are accepted.</summary>
internal static class SetUserPhoto
{
    public sealed record Command(Guid UserId, byte[]? Photo);

    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory, IEventPublisher events)
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

            var updated = await db.Users
                .Where(x => x.Id == command.UserId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Photo, command.Photo).SetProperty(x => x.PhotoThumbnail, thumbnail), cancellationToken)
                .ConfigureAwait(false);

            if (updated == 0)
            {
                return Error.NotFound("user_not_found", "User not found.");
            }

            await events.PublishAsync(new UserPhotoChanged(command.UserId), cancellationToken).ConfigureAwait(false);

            return command.UserId;
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Clients.Persistence;
using Dotnetstore.PersonalApp.Shared.Photos;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

/// <summary>Sets or, with a null photo, removes the contact person's picture. Only PNG and JPEG up to 6 MB are accepted.</summary>
internal static class SetContactPersonPhoto
{
    public sealed record Command(Guid ContactPersonId, byte[]? Photo);

    public sealed class Handler(IDbContextFactory<ClientsDbContext> dbFactory)
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

            var updated = await db.ContactPersons
                .Where(x => x.Id == command.ContactPersonId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Photo, command.Photo).SetProperty(x => x.PhotoThumbnail, thumbnail), cancellationToken)
                .ConfigureAwait(false);

            return updated == 0 ? Error.NotFound("contact_person_not_found", "Contact person not found.") : command.ContactPersonId;
        }
    }
}

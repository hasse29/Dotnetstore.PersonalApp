using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Sets or, with a null photo, removes the picture on the user's own CV. Only PNG and JPEG up to 6 MB are accepted.</summary>
internal static class SetCvPhoto
{
    public const int MaxPhotoBytes = 6 * 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    public sealed record Command(Guid CvId, Guid UserId, byte[]? Photo);

    public sealed class Handler(IDbContextFactory<CvsDbContext> dbFactory, TimeProvider clock)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.Photo is { } photo)
            {
                if (photo.Length > MaxPhotoBytes)
                {
                    return Error.Validation("cv_photo_too_large", "The photo is larger than 6 MB.");
                }

                if (!photo.AsSpan().StartsWith(PngSignature) && !photo.AsSpan().StartsWith(JpegSignature))
                {
                    return Error.Validation("cv_photo_invalid", "The photo must be a PNG or JPEG image.");
                }
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var cv = await db.Cvs.FirstOrDefaultAsync(c => c.Id == command.CvId && c.UserId == command.UserId, cancellationToken).ConfigureAwait(false);
            if (cv is null)
            {
                return Error.NotFound("cv_not_found", "CV not found.");
            }

            cv.Photo = command.Photo;
            cv.UpdatedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return cv.Id;
        }
    }
}

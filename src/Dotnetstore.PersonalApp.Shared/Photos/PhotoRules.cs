using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Shared.Photos;

/// <summary>What every module that stores a photo accepts: PNG or JPEG, at most 6 MB.</summary>
public static class PhotoRules
{
    public const int MaxBytes = 6 * 1024 * 1024;

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47];

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    /// <returns>An error (<c>photo_too_large</c> / <c>photo_invalid</c>), or null when the photo is acceptable. A null photo means "remove" and is acceptable.</returns>
    public static Error? Validate(ReadOnlySpan<byte> photo)
    {
        if (photo.Length > MaxBytes)
        {
            return Error.Validation("photo_too_large", "The photo is larger than 6 MB.");
        }

        if (photo.Length > 0 && !photo.StartsWith(PngSignature) && !photo.StartsWith(JpegSignature))
        {
            return Error.Validation("photo_invalid", "The photo must be a PNG or JPEG image.");
        }

        return null;
    }
}

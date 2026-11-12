using SkiaSharp;

namespace Dotnetstore.PersonalApp.Shared.Photos;

/// <summary>
/// Makes the small square picture that lists and the top bar show, so they never load the full photo (up to 6 MB per row).
/// </summary>
public static class PhotoThumbnail
{
    public const int Size = 96;

    /// <returns>A <see cref="Size"/> x <see cref="Size"/> PNG cropped from the middle of the photo, or null when the bytes cannot be decoded as an image.</returns>
    public static byte[]? Create(ReadOnlySpan<byte> photo)
    {
        using var source = TryDecode(photo);
        if (source is null || source.Width == 0 || source.Height == 0)
        {
            return null;
        }

        var side = Math.Min(source.Width, source.Height);
        var crop = SKRectI.Create((source.Width - side) / 2, (source.Height - side) / 2, side, side);

        using var thumbnail = new SKBitmap(Size, Size);
        using (var canvas = new SKCanvas(thumbnail))
        using (var sourceImage = SKImage.FromBitmap(source))
        using (var paint = new SKPaint { IsAntialias = true })
        {
            canvas.DrawImage(sourceImage, crop, SKRect.Create(Size, Size), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
        }

        using var image = SKImage.FromBitmap(thumbnail);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data?.ToArray();
    }

    // Skia throws ArgumentNullException (no codec) instead of returning null when the bytes are not a known image.
    private static SKBitmap? TryDecode(ReadOnlySpan<byte> photo)
    {
        try
        {
            return SKBitmap.Decode(photo);
        }
        catch (ArgumentNullException)
        {
            return null;
        }
    }
}

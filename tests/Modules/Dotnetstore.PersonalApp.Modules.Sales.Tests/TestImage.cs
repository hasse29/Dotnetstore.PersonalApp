using SkiaSharp;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>A real, decodable image, because saving a photo also makes its thumbnail.</summary>
internal static class TestImage
{
    public static byte[] Create(SKEncodedImageFormat format, int width = 200, int height = 120)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(format, 90).ToArray();
    }
}

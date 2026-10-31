namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Reads the pixel size of a PNG or JPEG straight from its header, so the Word export can keep the photo's proportions.</summary>
internal static class ImageSize
{
    public static bool IsPng(ReadOnlySpan<byte> image) => image.Length > 4 && image[0] == 0x89 && image[1] == 0x50 && image[2] == 0x4E && image[3] == 0x47;

    public static (int Width, int Height)? TryRead(byte[] image)
    {
        if (IsPng(image))
        {
            // IHDR follows the 8-byte signature and the chunk header: width and height are big-endian 32-bit values.
            return image.Length >= 24 ? (ReadInt32(image, 16), ReadInt32(image, 20)) : null;
        }

        // JPEG: walk the marker segments until a start-of-frame segment, which holds height then width.
        var position = 2;
        while (position + 9 < image.Length)
        {
            if (image[position] != 0xFF)
            {
                return null;
            }

            var marker = image[position + 1];
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return ((image[position + 7] << 8) | image[position + 8], (image[position + 5] << 8) | image[position + 6]);
            }

            position += 2 + ((image[position + 2] << 8) | image[position + 3]);
        }

        return null;
    }

    private static int ReadInt32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}

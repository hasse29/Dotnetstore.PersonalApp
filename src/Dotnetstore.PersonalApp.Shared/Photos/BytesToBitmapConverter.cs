using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Dotnetstore.PersonalApp.Shared.Photos;

/// <summary>Turns a photo's bytes into an image for the view, so view models never hold a UI type.</summary>
public sealed class BytesToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var bytes = value switch
        {
            byte[] array => array,
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            _ => [],
        };
        if (bytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidOperationException)
        {
            // The photo was validated when it was saved, but a corrupt file should show the initials, not break the page.
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

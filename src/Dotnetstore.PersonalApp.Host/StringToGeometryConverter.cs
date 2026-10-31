using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Dotnetstore.PersonalApp.Host;

/// <summary>Turns SVG path data (as exposed by <c>IModulePage.IconData</c>) into a <see cref="Geometry"/> for <c>PathIcon</c>.</summary>
internal sealed class StringToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string { Length: > 0 } data ? Geometry.Parse(data) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

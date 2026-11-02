using Avalonia;
using Avalonia.Controls;

namespace Dotnetstore.PersonalApp.Shared.Photos;

/// <summary>
/// A small round picture for list rows and bars: the photo when there is one, otherwise the initials on the brand gradient.
/// Bind it to a thumbnail, never to a full photo.
/// </summary>
public sealed partial class AvatarView : UserControl
{
    public static readonly StyledProperty<string?> InitialsProperty = AvaloniaProperty.Register<AvatarView, string?>(nameof(Initials));

    public static readonly StyledProperty<byte[]?> PhotoProperty = AvaloniaProperty.Register<AvatarView, byte[]?>(nameof(Photo));

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<AvatarView, double>(nameof(Size), 32);

    private readonly BytesToBitmapConverter _converter = new();

    public AvatarView()
    {
        InitializeComponent();
        Render();
    }

    public string? Initials
    {
        get => GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    // Avalonia properties of array type are the established way to hand image bytes to a control.
#pragma warning disable CA1819
    public byte[]? Photo
    {
        get => GetValue(PhotoProperty);
        set => SetValue(PhotoProperty, value);
    }
#pragma warning restore CA1819

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);

        base.OnPropertyChanged(change);

        if (change.Property == InitialsProperty || change.Property == PhotoProperty || change.Property == SizeProperty)
        {
            Render();
        }
    }

    private void Render()
    {
        var bitmap = _converter.Convert(Photo, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture);
        var radius = Size / 2;

        Fallback.Width = Fallback.Height = Size;
        Fallback.CornerRadius = new CornerRadius(radius);
        Fallback.IsVisible = bitmap is null;
        InitialsText.Text = Initials;
        InitialsText.FontSize = Math.Max(10, Size * 0.38);

        PictureBorder.Width = PictureBorder.Height = Size;
        PictureBorder.CornerRadius = new CornerRadius(radius);
        PictureBorder.IsVisible = bitmap is not null;
        Picture.Source = bitmap as Avalonia.Media.IImage;
    }
}

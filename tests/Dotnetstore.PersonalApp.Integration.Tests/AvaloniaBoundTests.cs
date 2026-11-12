using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.Files;
using Dotnetstore.PersonalApp.Host.Updates;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class AvaloniaBoundTests
{
    // A valid 1x1 PNG.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---- bytes to bitmap ----

    [Fact]
    public Task AValidImageBecomesABitmapWhetherGivenAsAnArrayOrMemory() => Headless.Run(() =>
    {
        var converter = new BytesToBitmapConverter();

        converter.Convert(Png, typeof(IImage), null, CultureInfo.InvariantCulture).ShouldBeOfType<Bitmap>().Dispose();
        converter.Convert(new ReadOnlyMemory<byte>(Png), typeof(IImage), null, CultureInfo.InvariantCulture).ShouldBeOfType<Bitmap>().Dispose();
    });

    [Fact]
    public Task NothingOrAnUnexpectedValueGivesNoImage() => Headless.Run(() =>
    {
        var converter = new BytesToBitmapConverter();

        converter.Convert(null, typeof(IImage), null, CultureInfo.InvariantCulture).ShouldBeNull();
        converter.Convert(Array.Empty<byte>(), typeof(IImage), null, CultureInfo.InvariantCulture).ShouldBeNull();
        converter.Convert(ReadOnlyMemory<byte>.Empty, typeof(IImage), null, CultureInfo.InvariantCulture).ShouldBeNull();
        converter.Convert("not bytes", typeof(IImage), null, CultureInfo.InvariantCulture).ShouldBeNull();
    });

    [Fact]
    public Task ACorruptImageNeverThrows() => Headless.Run(() =>
    {
        var converter = new BytesToBitmapConverter();

        Should.NotThrow(() => converter.Convert(new byte[] { 1, 2, 3, 4 }, typeof(IImage), null, CultureInfo.InvariantCulture));
    });

    [Fact]
    public void ConvertingBackIsNotSupported() =>
        Should.Throw<NotSupportedException>(() => new BytesToBitmapConverter().ConvertBack(null, typeof(byte[]), null, CultureInfo.InvariantCulture));

    // ---- string to geometry ----

    [Fact]
    public Task PathDataBecomesAGeometry() => Headless.Run(() =>
    {
        var result = new StringToGeometryConverter().Convert("M0,0 L10,0 L10,10 Z", typeof(Geometry), null, CultureInfo.InvariantCulture);

        result.ShouldBeAssignableTo<Geometry>()!.Bounds.Width.ShouldBe(10);
    });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoPathDataGivesNoGeometry(string? data) =>
        new StringToGeometryConverter().Convert(data, typeof(Geometry), null, CultureInfo.InvariantCulture).ShouldBeNull();

    [Fact]
    public void AValueThatIsNotAStringGivesNoGeometry() =>
        new StringToGeometryConverter().Convert(42, typeof(Geometry), null, CultureInfo.InvariantCulture).ShouldBeNull();

    [Fact]
    public void GeometryConvertingBackIsNotSupported() =>
        Should.Throw<NotSupportedException>(() => new StringToGeometryConverter().ConvertBack(null, typeof(string), null, CultureInfo.InvariantCulture));

    // ---- services with no main window ----
    // Without a desktop lifetime and main window there is nothing to show a dialog on; each service must say "nothing chosen"
    // instead of throwing.

    [Fact]
    public async Task TheImagePickerReturnsNothingWithoutAMainWindow() =>
        (await new AvaloniaFileOpenService().PickImageAsync("Pick", 1024, Ct)).ShouldBeNull();

    [Fact]
    public async Task TheFilePickerReturnsNoFilesWithoutAMainWindow()
    {
        var result = await new AvaloniaFilePickService().PickFilesAsync("Pick", 1024, Ct);

        result.Files.ShouldBeEmpty();
        result.Skipped.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheSaveDialogReturnsNothingWithoutAMainWindow() =>
        (await new AvaloniaFileSaveService().SaveAsync("Save", "report.xlsx", [1, 2, 3], Ct)).ShouldBeNull();

    [Fact]
    public async Task LaunchingAnAddressWithoutAMainWindowDoesNothing() =>
        await Should.NotThrowAsync(() => new AvaloniaUriLauncher().LaunchAsync(new Uri("https://example.com")));
}

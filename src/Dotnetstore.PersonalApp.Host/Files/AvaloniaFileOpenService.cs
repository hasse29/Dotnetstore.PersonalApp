using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Dotnetstore.PersonalApp.Shared.Files;

namespace Dotnetstore.PersonalApp.Host.Files;

internal sealed class AvaloniaFileOpenService : IFileOpenService
{
    public async Task<PickedFile?> PickImageAsync(string title, long maxBytes, CancellationToken cancellationToken)
    {
        // Looked up per call: the main window replaces the splash window after startup.
        var storage = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        });
        if (files.Count == 0)
        {
            return null;
        }

        await using var stream = await files[0].OpenReadAsync();
        if (stream.CanSeek && stream.Length > maxBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.Length > maxBytes ? null : new PickedFile(files[0].Name, buffer.ToArray());
    }
}

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Dotnetstore.PersonalApp.Shared.Files;

namespace Dotnetstore.PersonalApp.Host.Files;

internal sealed class AvaloniaFilePickService : IFilePickService
{
    public async Task<FilePickResult> PickFilesAsync(string title, long maxBytes, CancellationToken cancellationToken)
    {
        // Looked up per call: the main window replaces the splash window after startup.
        var storage = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider;
        if (storage is null)
        {
            return new FilePickResult([], []);
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = title, AllowMultiple = true });

        var picked = new List<PickedFile>();
        var skipped = new List<string>();
        foreach (var file in files)
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
                if (stream.CanSeek && stream.Length > maxBytes)
                {
                    skipped.Add(file.Name);
                    continue;
                }

                // Stop reading just past the limit so a stream that cannot report its length is never read whole.
                using var buffer = new MemoryStream();
                var chunk = new byte[81920];
                int read;
                while (buffer.Length <= maxBytes && (read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
                {
                    await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
                }

                if (buffer.Length > maxBytes)
                {
                    skipped.Add(file.Name);
                }
                else
                {
                    picked.Add(new PickedFile(file.Name, buffer.ToArray()));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                skipped.Add(file.Name);
            }
        }

        return new FilePickResult(picked, skipped);
    }
}

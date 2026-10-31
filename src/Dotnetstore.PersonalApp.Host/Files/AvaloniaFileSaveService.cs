using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Dotnetstore.PersonalApp.Shared.Files;

namespace Dotnetstore.PersonalApp.Host.Files;

internal sealed class AvaloniaFileSaveService : IFileSaveService
{
    public async Task<string?> SaveAsync(string title, string suggestedFileName, byte[] content, CancellationToken cancellationToken)
    {
        // Looked up per call: the main window replaces the splash window after startup.
        var storage = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var extension = Path.GetExtension(suggestedFileName);
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
            DefaultExtension = extension.TrimStart('.'),
            FileTypeChoices = [new FilePickerFileType(extension) { Patterns = [$"*{extension}"] }],
        });
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(content, cancellationToken);
        return file.TryGetLocalPath() ?? file.Name;
    }
}
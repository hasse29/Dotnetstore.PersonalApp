using Avalonia.Controls.ApplicationLifetimes;

namespace Dotnetstore.PersonalApp.Host.Updates;

internal sealed class AvaloniaUriLauncher : IUriLauncher
{
    public async Task LaunchAsync(Uri uri)
    {
        // Looked up per call: the main window replaces the splash window after startup.
        var window = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window?.Launcher is { } launcher)
        {
            await launcher.LaunchUriAsync(uri);
        }
    }
}

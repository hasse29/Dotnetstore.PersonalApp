using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;

namespace Dotnetstore.PersonalApp.Host.Backup;

/// <summary>Lets a view model restart the app (needed after a restore) without touching UI types.</summary>
internal interface IAppRestarter
{
    void Restart();
}

internal sealed class AvaloniaAppRestarter : IAppRestarter
{
    public void Restart()
    {
        if (Environment.ProcessPath is { } path)
        {
            var start = new ProcessStartInfo(path) { UseShellExecute = false };

            // Started with `dotnet <dll>` (development): the host program is dotnet, so the dll has to be passed again.
            if (Path.GetFileNameWithoutExtension(path).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                start.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            }

            using var process = Process.Start(start);
        }

        (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}

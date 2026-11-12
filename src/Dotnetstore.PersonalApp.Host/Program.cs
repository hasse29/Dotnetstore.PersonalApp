using Avalonia;
using Dotnetstore.PersonalApp.Host.Telemetry;
using Serilog;
using System;

namespace Dotnetstore.PersonalApp.Host;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var settings = UserSettings.Load(AppPaths.DataDirectory);
        using var telemetry = TelemetrySetup.Start(AppPaths.DataDirectory, settings);
        Avalonia.Logging.Logger.Sink = new AvaloniaLogSink(Avalonia.Logging.LogEventLevel.Warning);
        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Whatever ended the app is in the log before the process dies.
            Log.Fatal(ex, "The application failed");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont();
}

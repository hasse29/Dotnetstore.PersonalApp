using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using Dotnetstore.PersonalApp.Host.Resources;
using Dotnetstore.PersonalApp.Host.Startup;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Host.Views;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Host;

internal sealed partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            InitializeLocalization();

            var splashViewModel = new SplashViewModel();
            var splash = new SplashWindow { DataContext = splashViewModel };
            desktop.MainWindow = splash;
            desktop.Exit += (_, _) => _services?.Dispose();

            _ = StartAsync(desktop, splash, splashViewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Runs before any window exists, so even the splash screen is shown in the saved (or system) language.
    private static void InitializeLocalization()
    {
        var localizer = Localizer.Instance;
        localizer.AddResources(HostStrings.ResourceManager);

        var settings = UserSettings.Load(AppPaths.DataDirectory);
        localizer.SetCulture(settings.Culture ?? localizer.Match(CultureInfo.CurrentUICulture));
        localizer.CultureChanged += (_, _) =>
        {
            settings.Culture = localizer.Culture.Name;
            settings.Save(AppPaths.DataDirectory);
        };
    }

    // Startup failures must be shown to the user and end the app, so the broad catch is intentional.
#pragma warning disable CA1031
    private async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop, SplashWindow splash, SplashViewModel splashViewModel)
    {
        try
        {
            splashViewModel.Apply(new StartupProgress(0, Localizer.Instance.Get("Splash_Building")));
            _services = await Task.Run(() => CompositionRoot.BuildServices()).ConfigureAwait(true);

            var progress = new Progress<StartupProgress>(splashViewModel.Apply);
            await _services.GetRequiredService<StartupRunner>().RunAsync(progress, CancellationToken.None).ConfigureAwait(true);

            splashViewModel.Apply(new StartupProgress(1, Localizer.Instance.Get("Splash_CheckingUsers")));
            var mainViewModel = _services.GetRequiredService<MainWindowViewModel>();
            await mainViewModel.InitializeAsync(CancellationToken.None).ConfigureAwait(true);

            var mainWindow = new MainWindow { DataContext = mainViewModel };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            splash.Close();
        }
        catch (Exception ex)
        {
            splashViewModel.Fail(Localizer.Instance.Format("Startup_Failed", ex.Message));
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            desktop.Shutdown(1);
        }
    }
#pragma warning restore CA1031
}

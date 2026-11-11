using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class DashboardTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-dashboard-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private async Task<ServiceProvider> StartAsync()
    {
        var services = CompositionRoot.BuildServices(_directory);
        foreach (var task in services.GetServices<IStartupTask>())
        {
            await task.RunAsync(Ct);
        }

        return services;
    }

    [Fact]
    public async Task EveryModuleWidgetLoadsAgainstAFreshDatabaseAndTitlesItsPanel()
    {
        using var services = await StartAsync();
        var context = new DashboardContext(Guid.NewGuid(), new DateOnly(2026, 11, 4));

        var widgets = services.GetServices<IDashboardWidget>().ToList();
        var panels = new List<DashboardPanel>();
        foreach (var widget in widgets)
        {
            panels.Add(await widget.LoadAsync(context, Ct));
        }

        widgets.Select(w => w.ModuleKey).ShouldBe(["school"]);
        panels.ShouldAllBe(p => p.Title.Length > 0 && !p.Title.StartsWith("Missing", StringComparison.Ordinal) && p.Metrics.Count > 0);
        panels.ShouldAllBe(p => p.Rows.Count == 0 ? p.EmptyText.Length > 0 : true);
    }

    [Fact]
    public async Task TheDashboardIsTheFirstPageAndShowsOnePanelPerEnabledModule()
    {
        using var services = await StartAsync();
        var user = new AuthenticatedUser(Guid.NewGuid(), "teacher", "Test Teacher");
        services.GetRequiredService<Dotnetstore.PersonalApp.Host.Auth.CurrentUserAccessor>().User = user;

        services.GetServices<IModulePage>().First().ShouldBeOfType<DashboardPage>();

        var dashboard = services.GetRequiredService<DashboardViewModel>();
        await dashboard.LoadCommand.ExecuteAsync(null);

        dashboard.Panels.Count.ShouldBe(1);
        dashboard.SomeUnavailable.ShouldBeFalse();
        dashboard.Greeting.ShouldContain("Test Teacher");

        var settings = services.GetRequiredService<ModuleSettings>();
        settings.SetEnabled("school", false);
        await dashboard.LoadCommand.ExecuteAsync(null);

        dashboard.Panels.Count.ShouldBe(0);
        _ = services.GetRequiredService<ILocalizationService>();
    }
}

using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Shared.Products;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class BusinessModulesTests : IDisposable
{
    private static readonly string[] BusinessModules = ["production", "purchasing", "sales"];

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-business-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task EveryMigrationAppliesToAFreshDataDirectoryAndEveryPageCreatesItsViewModel()
    {
        using var services = CompositionRoot.BuildServices(_directory);

        foreach (var task in services.GetServices<IStartupTask>())
        {
            await task.RunAsync(Ct);
        }

        var pages = services.GetServices<IModulePage>().Where(p => p.ModuleKey is { } key && BusinessModules.Contains(key)).ToList();

        pages.Count.ShouldBe(36);
        pages.Select(p => p.Title).ShouldBeUnique();
        pages.ShouldAllBe(p => p.CreateViewModel() != null);
        BusinessModules.ShouldAllBe(key => File.Exists(Path.Combine(_directory, key + ".db")));
    }

    [Fact]
    public async Task ProductionAndPurchasingProvideTheLookupsOtherModulesUse()
    {
        using var services = CompositionRoot.BuildServices(_directory);
        foreach (var task in services.GetServices<IStartupTask>())
        {
            await task.RunAsync(Ct);
        }

        var units = await services.GetRequiredService<IUnitMeasureLookup>().ListAsync(Ct);
        var shipMethods = await services.GetRequiredService<IShipMethodLookup>().ListAsync(Ct);
        var productExists = await services.GetRequiredService<IProductLookup>().ExistsAsync(Guid.NewGuid(), Ct);

        units.Select(u => u.Name).ShouldContain("Kilogram");
        shipMethods.Select(s => s.Name).ShouldContain("Standard");
        productExists.ShouldBeFalse();
    }

    [Fact]
    public void TheBusinessModulesCanBeSwitchedOffOneByOne()
    {
        using var services = CompositionRoot.BuildServices(_directory);

        var listed = services.GetServices<ModuleInfo>().Select(m => m.Key).ToList();

        listed.ShouldContain("production");
        listed.ShouldContain("purchasing");
        listed.ShouldContain("sales");
    }
}

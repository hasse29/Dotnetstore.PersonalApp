using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.Startup;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class SplashViewModelTests
{
    [Fact]
    public void ProgressIsShownAsAPercentageWithItsMessage()
    {
        var viewModel = new SplashViewModel();

        viewModel.Apply(new StartupProgress(0.25, "Preparing clients database..."));

        viewModel.Progress.ShouldBe(25);
        viewModel.Status.ShouldBe("Preparing clients database...");
        viewModel.HasFailed.ShouldBeFalse();
    }

    [Fact]
    public void AFailureIsFlaggedWithItsMessage()
    {
        var viewModel = new SplashViewModel();

        viewModel.Fail("Startup failed: disk full");

        viewModel.HasFailed.ShouldBeTrue();
        viewModel.Status.ShouldBe("Startup failed: disk full");
    }
}

public sealed class ModulesViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-modules-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private ModulesViewModel NewViewModel(out ModuleSettings settings)
    {
        Directory.CreateDirectory(_directory);
        settings = new ModuleSettings(_directory);
        return new ModulesViewModel(
            [new ModuleInfo("clients", "Clients_Title"), new ModuleInfo("audit", "Audit_Title", CanDisable: false)],
            settings,
            new Localizer());
    }

    [Fact]
    public void ListsEveryModuleAsEnabledByDefaultAndKnowsWhichCannotBeSwitchedOff()
    {
        var viewModel = NewViewModel(out _);

        viewModel.Items.Count.ShouldBe(2);
        viewModel.Items.ShouldAllBe(item => item.IsEnabled);
        viewModel.Items[0].CanDisable.ShouldBeTrue();
        viewModel.Items[1].CanDisable.ShouldBeFalse();
    }

    [Fact]
    public void SwitchingAModuleOffIsRememberedAcrossRestarts()
    {
        var viewModel = NewViewModel(out var settings);

        viewModel.Items[0].IsEnabled = false;

        settings.IsEnabled("clients").ShouldBeFalse();
        new ModuleSettings(_directory).IsEnabled("clients").ShouldBeFalse();

        viewModel.Items[0].IsEnabled = true;
        new ModuleSettings(_directory).IsEnabled("clients").ShouldBeTrue();
    }
}

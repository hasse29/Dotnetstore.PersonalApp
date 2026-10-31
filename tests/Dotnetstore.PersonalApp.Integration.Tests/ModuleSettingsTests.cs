using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class ModuleSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-modules-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void DisabledModulesArePersisted()
    {
        new ModuleSettings(_directory).SetEnabled("clients", false);

        var reloaded = new ModuleSettings(_directory);

        Assert.False(reloaded.IsEnabled("clients"));
        Assert.True(reloaded.IsEnabled("users"));
    }

    [Fact]
    public void ShellHidesPagesOfDisabledModules()
    {
        var settings = new ModuleSettings(_directory);
        IModulePage[] pages = [new TestPage("A", "a"), new TestPage("B", "b"), new TestPage("Always", null)];
        using var shell = new ShellViewModel(pages, new AuthenticatedUser(Guid.NewGuid(), "u", "User"), () => { }, settings);

        settings.SetEnabled("a", false);

        Assert.DoesNotContain(shell.Entries, entry => entry.Title == "A");
        Assert.Equal("B", shell.SelectedPage?.Title);

        settings.SetEnabled("a", true);

        Assert.Contains(shell.Entries, entry => entry.Title == "A");
    }

    private sealed class TestPage(string title, string? moduleKey) : IModulePage
    {
        public string Title => title;

        public string IconData => string.Empty;

        public string? ModuleKey => moduleKey;

        public ViewModelBase CreateViewModel() => new PageViewModel();
    }

    private sealed class PageViewModel : ViewModelBase;
}
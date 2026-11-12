using Avalonia.Threading;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class ShellNavigationTests
{
    private static readonly ModulePageGroup Group = new("tools", "Tools", string.Empty, 1);

    private static ShellViewModel NewShell() => new(
        [new TestPage("Home", null), new TestPage("One", Group), new TestPage("Two", Group)],
        new AuthenticatedUser(Guid.NewGuid(), "u", "User"),
        () => { });

    [Fact]
    public void ClickingAGroupHeaderLeavesTheListAloneUntilTheClickIsHandled()
    {
        using var shell = NewShell();
        var header = shell.Entries.Single(entry => entry.IsHeader);
        shell.Entries.ShouldNotContain(entry => entry.Title == "One");
        var rowsBefore = shell.Entries.Count;

        // The ListBox sets SelectedItem while it is still committing the click; changing its items then crashes the app.
        shell.SelectedEntry = header;

        shell.Entries.Count.ShouldBe(rowsBefore);

        Dispatcher.UIThread.RunJobs();

        shell.Entries.ShouldContain(entry => entry.Title == "One");
        shell.Entries.ShouldContain(entry => entry.Title == "Two");
        shell.SelectedPage?.Title.ShouldBe("Home");
    }

    [Fact]
    public void ClickingAHeaderAgainCollapsesTheGroup()
    {
        using var shell = NewShell();
        var header = shell.Entries.Single(entry => entry.IsHeader);
        shell.SelectedEntry = header;
        Dispatcher.UIThread.RunJobs();

        shell.SelectedEntry = shell.Entries.Single(entry => entry.IsHeader);
        Dispatcher.UIThread.RunJobs();

        shell.Entries.ShouldNotContain(entry => entry.Title == "One");
    }

    [Fact]
    public void ClickingAPageOpensItAfterTheClickIsHandled()
    {
        using var shell = NewShell();
        shell.SelectedEntry = shell.Entries.Single(entry => entry.IsHeader);
        Dispatcher.UIThread.RunJobs();

        shell.SelectedEntry = shell.Entries.Single(entry => entry.Title == "Two");
        shell.SelectedPage?.Title.ShouldBe("Home");

        Dispatcher.UIThread.RunJobs();

        shell.SelectedPage?.Title.ShouldBe("Two");
        shell.CurrentPage.ShouldNotBeNull();
    }

    private sealed class TestPage(string title, ModulePageGroup? group) : IModulePage
    {
        public string Title => title;

        public string IconData => string.Empty;

        public string? ModuleKey => null;

        public ModulePageGroup? Group => group;

        public ViewModelBase CreateViewModel() => new PageViewModel();
    }

    private sealed class PageViewModel : ViewModelBase;
}

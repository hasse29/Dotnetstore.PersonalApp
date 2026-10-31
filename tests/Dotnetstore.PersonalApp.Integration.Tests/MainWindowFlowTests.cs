using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class MainWindowFlowTests
{
    private static readonly AuthenticatedUser Ada = new(Guid.NewGuid(), "ada@example.com", "Ada Lovelace");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LandsOnFirstUserSetupWhenThereAreNoUsers()
    {
        var pages = new FakeAuthenticationPages(hasUsers: false);
        var viewModel = new MainWindowViewModel(pages, [], new Localizer());

        await viewModel.InitializeAsync(Ct);

        Assert.IsType<FakeFirstUserPage>(viewModel.Content);
    }

    [Fact]
    public async Task LandsOnLoginWhenUsersExist()
    {
        var pages = new FakeAuthenticationPages(hasUsers: true);
        var viewModel = new MainWindowViewModel(pages, [], new Localizer());

        await viewModel.InitializeAsync(Ct);

        Assert.IsType<FakeLoginPage>(viewModel.Content);
    }

    [Fact]
    public async Task CreatingTheFirstUserLeadsToLogin()
    {
        var pages = new FakeAuthenticationPages(hasUsers: false);
        var viewModel = new MainWindowViewModel(pages, [], new Localizer());
        await viewModel.InitializeAsync(Ct);

        ((FakeFirstUserPage)viewModel.Content!).Complete();

        Assert.IsType<FakeLoginPage>(viewModel.Content);
    }

    [Fact]
    public async Task LoggingInShowsTheShellForThatUserAndSigningOutReturnsToLogin()
    {
        var pages = new FakeAuthenticationPages(hasUsers: true);
        var viewModel = new MainWindowViewModel(pages, [new FakeModulePage()], new Localizer());
        await viewModel.InitializeAsync(Ct);

        ((FakeLoginPage)viewModel.Content!).LogIn(Ada);

        var shell = Assert.IsType<ShellViewModel>(viewModel.Content);
        Assert.Equal(Ada, shell.User);
        Assert.IsType<FakePageViewModel>(shell.CurrentPage);

        shell.SignOutCommand.Execute(null);

        Assert.IsType<FakeLoginPage>(viewModel.Content);
    }

    private sealed class FakeAuthenticationPages(bool hasUsers) : IAuthenticationPages
    {
        public Task<bool> HasUsersAsync(CancellationToken cancellationToken) => Task.FromResult(hasUsers);

        public ViewModelBase CreateFirstUserPage(Action onCreated) => new FakeFirstUserPage(onCreated);

        public ViewModelBase CreateLoginPage(Action<AuthenticatedUser> onLoggedIn) => new FakeLoginPage(onLoggedIn);
    }

    private sealed class FakeFirstUserPage(Action onCreated) : ViewModelBase
    {
        public void Complete() => onCreated();
    }

    private sealed class FakeLoginPage(Action<AuthenticatedUser> onLoggedIn) : ViewModelBase
    {
        public void LogIn(AuthenticatedUser user) => onLoggedIn(user);
    }

    private sealed class FakePageViewModel : ViewModelBase;

    private sealed class FakeModulePage : IModulePage
    {
        public string Title => "Fake";

        public string IconData => string.Empty;

        public ViewModelBase CreateViewModel() => new FakePageViewModel();
    }
}

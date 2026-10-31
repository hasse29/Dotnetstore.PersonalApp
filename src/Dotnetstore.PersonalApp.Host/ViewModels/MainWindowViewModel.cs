using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Host.Auth;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>
/// Owns the top-level flow: first-user setup (only when no users exist) -> login -> shell. Signing out returns to login.
/// Changing language rebuilds the current screen so every string is looked up again.
/// </summary>
internal sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthenticationPages _authentication;
    private readonly IEnumerable<IModulePage> _pages;
    private readonly CurrentUserAccessor? _currentUser;
    private Action? _showCurrent;

    public MainWindowViewModel(IAuthenticationPages authentication, IEnumerable<IModulePage> pages, ILocalizationService localization, CurrentUserAccessor? currentUser = null)
    {
        _currentUser = currentUser;
        _authentication = authentication;
        _pages = pages;
        localization.CultureChanged += (_, _) => Dispatcher.UIThread.Post(() => _showCurrent?.Invoke());
    }

    [ObservableProperty]
    public partial ViewModelBase? Content { get; set; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (await _authentication.HasUsersAsync(cancellationToken))
        {
            ShowLogin();
        }
        else
        {
            ShowFirstUserSetup();
        }
    }

    private void ShowFirstUserSetup()
    {
        _showCurrent = ShowFirstUserSetup;
        Content = _authentication.CreateFirstUserPage(ShowLogin);
    }

    private void ShowLogin()
    {
        _showCurrent = ShowLogin;
        SetCurrentUser(null);
        Content = _authentication.CreateLoginPage(ShowShell);
    }

    private void SetCurrentUser(AuthenticatedUser? user)
    {
        if (_currentUser is not null)
        {
            _currentUser.User = user;
        }
    }

    private void ShowShell(AuthenticatedUser user)
    {
        _showCurrent = () => ShowShell(user);
        SetCurrentUser(user);
        Content = new ShellViewModel(_pages, user, ShowLogin);
    }
}

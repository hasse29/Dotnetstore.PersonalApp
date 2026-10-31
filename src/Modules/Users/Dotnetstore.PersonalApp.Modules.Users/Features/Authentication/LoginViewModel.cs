using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;

internal sealed partial class LoginViewModel(
    Login.Handler login,
    ILocalizationService localization,
    Action<AuthenticatedUser> onLoggedIn) : ViewModelBase
{
    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = await login.HandleAsync(new Login.Command(Username, Password), cancellationToken);

        if (result.IsSuccess)
        {
            Password = string.Empty;
            onLoggedIn(result.Value);
        }
        else
        {
            Password = string.Empty;
            ErrorMessage = localization.Describe(result.Error);
        }
    }
}

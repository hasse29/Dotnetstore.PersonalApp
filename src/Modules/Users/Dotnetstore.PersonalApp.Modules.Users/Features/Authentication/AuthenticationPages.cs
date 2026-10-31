using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;

internal sealed class AuthenticationPages(
    HasUsers.Handler hasUsers,
    Login.Handler login,
    CreateUser.Handler createUser,
    ILocalizationService localization) : IAuthenticationPages
{
    public Task<bool> HasUsersAsync(CancellationToken cancellationToken) => hasUsers.HandleAsync(cancellationToken);

    public ViewModelBase CreateFirstUserPage(Action onCreated) => new FirstUserViewModel(createUser, localization, onCreated);

    public ViewModelBase CreateLoginPage(Action<AuthenticatedUser> onLoggedIn) => new LoginViewModel(login, localization, onLoggedIn);
}

using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Auth;

/// <summary>
/// The pages the host shows before the shell: first-user setup and login. Implemented by the module that owns users,
/// so the host can drive the flow without referencing the module's view models.
/// </summary>
public interface IAuthenticationPages
{
    Task<bool> HasUsersAsync(CancellationToken cancellationToken);

    ViewModelBase CreateFirstUserPage(Action onCreated);

    ViewModelBase CreateLoginPage(Action<AuthenticatedUser> onLoggedIn);
}

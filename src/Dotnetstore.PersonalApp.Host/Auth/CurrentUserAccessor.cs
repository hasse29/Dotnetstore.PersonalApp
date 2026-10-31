using Dotnetstore.PersonalApp.Shared.Auth;

namespace Dotnetstore.PersonalApp.Host.Auth;

/// <summary>Holds the signed-in user for the whole app; set by the main window when login succeeds and cleared on sign out.</summary>
internal sealed class CurrentUserAccessor : ICurrentUser
{
    public AuthenticatedUser? User { get; set; }
}
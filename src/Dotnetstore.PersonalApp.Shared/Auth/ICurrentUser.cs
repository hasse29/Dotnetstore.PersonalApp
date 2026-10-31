namespace Dotnetstore.PersonalApp.Shared.Auth;

/// <summary>The signed-in user, for modules that record who did something. <c>null</c> while nobody is signed in.</summary>
public interface ICurrentUser
{
    AuthenticatedUser? User { get; }
}
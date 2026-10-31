namespace Dotnetstore.PersonalApp.Shared.Auth;

public sealed record AuthenticatedUser(Guid Id, string Username, string FullName);

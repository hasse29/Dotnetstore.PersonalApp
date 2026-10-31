namespace Dotnetstore.PersonalApp.Shared.Results;

/// <summary>
/// A failure a handler reports. <see cref="Code"/> is stable and module-scoped (e.g. <c>user_not_found</c>); the UI
/// translates it through the <c>Error_{code}</c> resource and only falls back to the English <see cref="Message"/>.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static Error NotFound(string code, string message) => new(code, message);

    public static Error Conflict(string code, string message) => new(code, message);

    public static Error InvalidCredentials() => new("invalid_credentials", "Invalid username or password.");

    public static Error Validation(string code, string message) => new(code, message);
}

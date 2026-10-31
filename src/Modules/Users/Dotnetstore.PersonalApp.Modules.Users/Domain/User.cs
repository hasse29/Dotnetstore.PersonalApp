namespace Dotnetstore.PersonalApp.Modules.Users.Domain;

internal sealed class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

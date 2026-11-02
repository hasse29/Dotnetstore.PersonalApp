namespace Dotnetstore.PersonalApp.Modules.Users.Domain;

internal sealed class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    /// <summary>PNG or JPEG bytes; read and written on their own so lists do not load them.</summary>
    public byte[]? Photo { get; set; }

    /// <summary>A small square copy of <see cref="Photo"/> for lists and bars.</summary>
    public byte[]? PhotoThumbnail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

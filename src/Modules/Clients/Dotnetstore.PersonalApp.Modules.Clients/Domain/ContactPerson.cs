namespace Dotnetstore.PersonalApp.Modules.Clients.Domain;

/// <summary>A person at a client company. Its addresses, phone numbers and email addresses live in other modules, keyed by <see cref="Id"/>.</summary>
internal sealed class ContactPerson
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? JobTitle { get; set; }

    public string? Notes { get; set; }

    public byte[]? Photo { get; set; }

    /// <summary>A small square copy of <see cref="Photo"/> for lists.</summary>
    public byte[]? PhotoThumbnail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

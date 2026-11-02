namespace Dotnetstore.PersonalApp.Modules.Clients.Domain;

/// <summary>A client is always a company.</summary>
internal sealed class Client
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OrganizationNumber { get; set; } = string.Empty;

    public string? VatNumber { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? Notes { get; set; }

    /// <summary>PNG or JPEG bytes; read and written on their own so lists do not load them.</summary>
    public byte[]? Photo { get; set; }

    /// <summary>A small square copy of <see cref="Photo"/> for lists and bars.</summary>
    public byte[]? PhotoThumbnail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

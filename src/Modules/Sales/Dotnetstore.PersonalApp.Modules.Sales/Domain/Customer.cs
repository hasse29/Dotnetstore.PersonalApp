namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>Someone the owner sells to: a company or a private person. Offers copy the name and address, so editing a customer never changes an issued document.</summary>
internal sealed class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Organization number for a company; empty for a private person.</summary>
    public string? OrganizationNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public byte[]? Photo { get; set; }

    /// <summary>A small square copy of <see cref="Photo"/> for lists.</summary>
    public byte[]? PhotoThumbnail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

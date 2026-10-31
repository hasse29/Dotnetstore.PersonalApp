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

    public DateTimeOffset CreatedAt { get; set; }
}

namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>Someone the owner buys products from.</summary>
internal sealed class Supplier
{
    public Guid Id { get; set; }

    /// <summary>Unique, ignoring case.</summary>
    public string Name { get; set; } = string.Empty;

    public string? OrganizationNumber { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? Notes { get; set; }

    /// <summary>Inactive suppliers keep their prices but are no longer offered when adding new ones.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public List<SupplierPrice> Prices { get; set; } = [];
}

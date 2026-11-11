namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>A place where products are kept: a shelf, a box, a room.</summary>
internal sealed class StorageLocation
{
    public Guid Id { get; set; }

    /// <summary>Unique, ignoring case.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Inactive locations keep their stock but are not offered for new receipts and transfers.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}

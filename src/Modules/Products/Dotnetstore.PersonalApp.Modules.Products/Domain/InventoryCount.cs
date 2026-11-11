namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>A stocktake: the quantities counted on the shelf, compared with the system and posted as adjustments.</summary>
internal sealed class InventoryCount
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Null when every location is counted.</summary>
    public Guid? LocationId { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null while the count is open; once set the count is final.</summary>
    public DateTimeOffset? PostedAt { get; set; }

    public List<InventoryCountLine> Lines { get; set; } = [];
}

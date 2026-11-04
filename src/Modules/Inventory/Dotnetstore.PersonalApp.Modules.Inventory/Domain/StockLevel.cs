namespace Dotnetstore.PersonalApp.Modules.Inventory.Domain;

/// <summary>stock level.</summary>
internal sealed class StockLevel
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid LocationId { get; set; }

    public StorageLocation Location { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal? MinQuantity { get; set; }
}
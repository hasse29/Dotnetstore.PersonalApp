namespace Dotnetstore.PersonalApp.Modules.Inventory.Domain;

/// <summary>count line.</summary>
internal sealed class InventoryCountLine
{
    public Guid Id { get; set; }

    public Guid InventoryCountId { get; set; }

    public InventoryCount InventoryCount { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Guid LocationId { get; set; }

    public StorageLocation Location { get; set; } = null!;

    public decimal ExpectedQty { get; set; }

    public decimal? CountedQty { get; set; }

    public decimal Difference { get; set; }
}
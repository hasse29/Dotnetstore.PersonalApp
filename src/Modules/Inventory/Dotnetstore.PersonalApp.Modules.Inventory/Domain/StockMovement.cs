namespace Dotnetstore.PersonalApp.Modules.Inventory.Domain;

/// <summary>stock movement.</summary>
internal sealed class StockMovement
{
    public Guid Id { get; set; }

    public DateOnly Date { get; set; }

    public Guid ProductId { get; set; }

    public Guid LocationId { get; set; }

    public StorageLocation Location { get; set; } = null!;

    public string Type { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string? Reference { get; set; }

    public string? Note { get; set; }
}
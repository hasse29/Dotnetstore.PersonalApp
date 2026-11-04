namespace Dotnetstore.PersonalApp.Modules.Inventory.Domain;

/// <summary>inventory count.</summary>
internal sealed class InventoryCount
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public DateOnly Date { get; set; }

    public string? Note { get; set; }

    public bool Posted { get; set; }
}
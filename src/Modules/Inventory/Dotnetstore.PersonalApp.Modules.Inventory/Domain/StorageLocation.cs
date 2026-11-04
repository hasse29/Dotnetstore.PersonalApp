namespace Dotnetstore.PersonalApp.Modules.Inventory.Domain;

/// <summary>storage location.</summary>
internal sealed class StorageLocation
{
    public Guid Id { get; set; }

    public Guid WarehouseId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public string? Zone { get; set; }

    public string? Description { get; set; }

    public decimal? Capacity { get; set; }
}
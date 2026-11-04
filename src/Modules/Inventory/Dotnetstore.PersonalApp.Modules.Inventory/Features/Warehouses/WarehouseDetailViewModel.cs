using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;

/// <summary>A warehouse with its storage locations and inventory counts.</summary>
internal sealed class WarehouseDetailViewModel(
    WarehouseDto warehouse,
    StorageLocationsViewModel locations,
    InventoryCountsViewModel counts) : DetailViewModel
{
    public WarehouseDto Warehouse { get; } = warehouse;

    public StorageLocationsViewModel Locations { get; } = locations.ForParent(warehouse.Id);

    public InventoryCountsViewModel Counts { get; } = counts.ForParent(warehouse.Id);
}

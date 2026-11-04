using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

internal sealed record StorageLocationDto(Guid Id, Guid WarehouseId, string WarehouseName, string Code, string? Zone, string? Description, decimal? Capacity)
{
    public static Expression<Func<StorageLocation, StorageLocationDto>> Projection { get; } =
        t => new StorageLocationDto(t.Id, t.WarehouseId, t.Warehouse.Name, t.Code, t.Zone, t.Description, t.Capacity);
}
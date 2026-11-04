using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

internal sealed record InventoryCountDto(Guid Id, string Number, Guid WarehouseId, string WarehouseName, DateOnly Date, string? Note, bool Posted)
{
    public static Expression<Func<InventoryCount, InventoryCountDto>> Projection { get; } =
        t => new InventoryCountDto(t.Id, t.Number, t.WarehouseId, t.Warehouse.Name, t.Date, t.Note, t.Posted);
}
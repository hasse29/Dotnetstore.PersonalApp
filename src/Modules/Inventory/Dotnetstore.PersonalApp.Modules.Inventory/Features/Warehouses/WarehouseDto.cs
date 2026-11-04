using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;

internal sealed record WarehouseDto(Guid Id, string Code, string Name, string? Address, bool Active)
{
    public static Expression<Func<Warehouse, WarehouseDto>> Projection { get; } =
        t => new WarehouseDto(t.Id, t.Code, t.Name, t.Address, t.Active);
}
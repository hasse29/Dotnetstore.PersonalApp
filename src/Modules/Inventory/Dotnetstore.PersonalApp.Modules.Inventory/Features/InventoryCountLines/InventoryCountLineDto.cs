using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;

internal sealed record InventoryCountLineDto(Guid Id, Guid InventoryCountId, string InventoryCountName, Guid ProductId, string ProductName, Guid LocationId, string LocationName, decimal ExpectedQty, decimal? CountedQty, decimal Difference)
{
    public static Expression<Func<InventoryCountLine, InventoryCountLineDto>> Projection { get; } =
        t => new InventoryCountLineDto(t.Id, t.InventoryCountId, t.InventoryCount.Number, t.ProductId, string.Empty, t.LocationId, t.Location.Code, t.ExpectedQty, t.CountedQty, t.Difference);

    public InventoryCountLineDto WithNames(IReadOnlyDictionary<Guid, string> productNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty) };
}
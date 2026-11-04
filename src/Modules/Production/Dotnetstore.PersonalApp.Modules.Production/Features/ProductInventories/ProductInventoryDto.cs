using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;

internal sealed record ProductInventoryDto(Guid Id, Guid ProductId, string ProductName, Guid LocationId, string LocationName, string Shelf, int Bin, int Quantity)
{
    public static Expression<Func<ProductInventory, ProductInventoryDto>> Projection { get; } =
        t => new ProductInventoryDto(t.Id, t.ProductId, t.Product.Name, t.LocationId, t.Location.Name, t.Shelf, t.Bin, t.Quantity);
}
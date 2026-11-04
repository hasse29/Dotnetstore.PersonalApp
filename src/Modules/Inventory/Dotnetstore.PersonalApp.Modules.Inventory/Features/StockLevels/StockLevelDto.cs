using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;

internal sealed record StockLevelDto(Guid Id, Guid ProductId, string ProductName, Guid LocationId, string LocationName, decimal Quantity, decimal? MinQuantity)
{
    public static Expression<Func<StockLevel, StockLevelDto>> Projection { get; } =
        t => new StockLevelDto(t.Id, t.ProductId, string.Empty, t.LocationId, t.Location.Code, t.Quantity, t.MinQuantity);

    public StockLevelDto WithNames(IReadOnlyDictionary<Guid, string> productNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty) };
}
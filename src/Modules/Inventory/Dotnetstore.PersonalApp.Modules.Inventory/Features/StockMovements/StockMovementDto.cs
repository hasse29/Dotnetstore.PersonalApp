using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

internal sealed record StockMovementDto(Guid Id, DateOnly Date, Guid ProductId, string ProductName, Guid LocationId, string LocationName, string Type, decimal Quantity, string? Reference, string? Note)
{
    public static Expression<Func<StockMovement, StockMovementDto>> Projection { get; } =
        t => new StockMovementDto(t.Id, t.Date, t.ProductId, string.Empty, t.LocationId, t.Location.Code, t.Type, t.Quantity, t.Reference, t.Note);

    public StockMovementDto WithNames(IReadOnlyDictionary<Guid, string> productNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty) };
}
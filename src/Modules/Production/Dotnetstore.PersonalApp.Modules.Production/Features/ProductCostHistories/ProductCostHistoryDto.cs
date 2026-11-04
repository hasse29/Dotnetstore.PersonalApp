using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;

internal sealed record ProductCostHistoryDto(Guid Id, Guid ProductId, string ProductName, DateOnly StartDate, DateOnly? EndDate, decimal StandardCost)
{
    public static Expression<Func<ProductCostHistory, ProductCostHistoryDto>> Projection { get; } =
        t => new ProductCostHistoryDto(t.Id, t.ProductId, t.Product.Name, t.StartDate, t.EndDate, t.StandardCost);
}
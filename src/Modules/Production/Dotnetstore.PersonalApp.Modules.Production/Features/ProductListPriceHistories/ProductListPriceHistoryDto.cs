using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductListPriceHistories;

internal sealed record ProductListPriceHistoryDto(Guid Id, Guid ProductId, string ProductName, DateOnly StartDate, DateOnly? EndDate, decimal ListPrice)
{
    public static Expression<Func<ProductListPriceHistory, ProductListPriceHistoryDto>> Projection { get; } =
        t => new ProductListPriceHistoryDto(t.Id, t.ProductId, t.Product.Name, t.StartDate, t.EndDate, t.ListPrice);
}
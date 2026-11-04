using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal sealed record ProductDto(Guid Id, string Name, string ProductNumber, bool MakeFlag, bool FinishedGoodsFlag, string? Color, int SafetyStockLevel, int ReorderPoint, decimal StandardCost, decimal ListPrice, string? Size, Guid? SizeUnitId, string? SizeUnitName, decimal? Weight, Guid? WeightUnitId, string? WeightUnitName, int DaysToManufacture, string ProductLine, string Class, string Style, Guid? SubcategoryId, string? SubcategoryName, Guid? ModelId, string? ModelName, DateOnly SellStartDate, DateOnly? SellEndDate, DateOnly? DiscontinuedDate)
{
    public static Expression<Func<Product, ProductDto>> Projection { get; } =
        t => new ProductDto(t.Id, t.Name, t.ProductNumber, t.MakeFlag, t.FinishedGoodsFlag, t.Color, t.SafetyStockLevel, t.ReorderPoint, t.StandardCost, t.ListPrice, t.Size, t.SizeUnitId, t.SizeUnit == null ? null : t.SizeUnit.Name, t.Weight, t.WeightUnitId, t.WeightUnit == null ? null : t.WeightUnit.Name, t.DaysToManufacture, t.ProductLine, t.Class, t.Style, t.SubcategoryId, t.Subcategory == null ? null : t.Subcategory.Name, t.ModelId, t.Model == null ? null : t.Model.Name, t.SellStartDate, t.SellEndDate, t.DiscontinuedDate);
}
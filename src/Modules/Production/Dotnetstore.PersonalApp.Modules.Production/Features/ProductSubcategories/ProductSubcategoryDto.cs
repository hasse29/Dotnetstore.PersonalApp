using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;

internal sealed record ProductSubcategoryDto(Guid Id, Guid CategoryId, string CategoryName, string Name)
{
    public static Expression<Func<ProductSubcategory, ProductSubcategoryDto>> Projection { get; } =
        t => new ProductSubcategoryDto(t.Id, t.CategoryId, t.Category.Name, t.Name);
}
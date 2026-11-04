using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCategories;

internal sealed record ProductCategoryDto(Guid Id, string Name)
{
    public static Expression<Func<ProductCategory, ProductCategoryDto>> Projection { get; } =
        t => new ProductCategoryDto(t.Id, t.Name);
}
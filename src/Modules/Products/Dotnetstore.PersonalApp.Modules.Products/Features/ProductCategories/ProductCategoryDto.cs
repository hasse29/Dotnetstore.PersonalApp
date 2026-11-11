using Dotnetstore.PersonalApp.Modules.Products.Domain;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;

internal sealed record ProductCategoryDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static ProductCategoryDto From(ProductCategory type) => new(type.Id, type.Name, type.Description, type.CreatedAt);
}

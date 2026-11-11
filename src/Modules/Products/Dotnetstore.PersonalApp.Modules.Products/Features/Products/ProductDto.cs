using Dotnetstore.PersonalApp.Modules.Products.Domain;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <param name="CategoryName">Null when the product has no category.</param>
internal sealed record ProductDto(
    Guid Id,
    string Number,
    string Name,
    ProductKind Kind,
    Guid? CategoryId,
    string? CategoryName,
    string? Description,
    bool IsActive,
    ProductUnit Unit,
    decimal UnitPrice,
    decimal CostPrice,
    decimal VatRate,
    string? Manufacturer,
    string? Model,
    string? SoftwareVersion,
    decimal? MinimumHours,
    decimal? MinimumStock,
    byte[]? Thumbnail)
{
    public static ProductDto From(Product product, string? categoryName) => new(
        product.Id,
        product.Number,
        product.Name,
        product.Kind,
        product.CategoryId,
        categoryName,
        product.Description,
        product.IsActive,
        product.Unit,
        product.UnitPrice,
        product.CostPrice,
        product.VatRate,
        product.Manufacturer,
        product.Model,
        product.SoftwareVersion,
        product.MinimumHours,
        product.MinimumStock,
        product.PhotoThumbnail);
}

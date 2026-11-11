namespace Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;

internal sealed record SupplierPriceDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    bool SupplierIsActive,
    Guid ProductId,
    string? SupplierArticleNumber,
    decimal Price,
    DateTimeOffset UpdatedAt);

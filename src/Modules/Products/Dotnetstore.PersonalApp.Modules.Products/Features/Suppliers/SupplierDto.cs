using Dotnetstore.PersonalApp.Modules.Products.Domain;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal sealed record SupplierDto(
    Guid Id,
    string Name,
    string? OrganizationNumber,
    string? Email,
    string? Phone,
    string? Website,
    string? Notes,
    bool IsActive)
{
    public static SupplierDto From(Supplier supplier) => new(
        supplier.Id,
        supplier.Name,
        supplier.OrganizationNumber,
        supplier.Email,
        supplier.Phone,
        supplier.Website,
        supplier.Notes,
        supplier.IsActive);
}

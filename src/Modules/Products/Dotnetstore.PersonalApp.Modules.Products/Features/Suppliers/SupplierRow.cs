namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

/// <summary>A supplier as the list shows it: the DTO plus its status in the current UI language.</summary>
internal sealed record SupplierRow(SupplierDto Supplier, string StatusText);
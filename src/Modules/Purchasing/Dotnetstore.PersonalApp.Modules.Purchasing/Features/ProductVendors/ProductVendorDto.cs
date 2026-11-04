using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;

internal sealed record ProductVendorDto(Guid Id, Guid ProductId, string ProductName, Guid VendorId, string VendorName, int AverageLeadTime, decimal StandardPrice, decimal? LastReceiptCost, DateOnly? LastReceiptDate, int MinOrderQty, int MaxOrderQty, int? OnOrderQty, Guid UnitId, string UnitName)
{
    public static Expression<Func<ProductVendor, ProductVendorDto>> Projection { get; } =
        t => new ProductVendorDto(t.Id, t.ProductId, string.Empty, t.VendorId, t.Vendor.Name, t.AverageLeadTime, t.StandardPrice, t.LastReceiptCost, t.LastReceiptDate, t.MinOrderQty, t.MaxOrderQty, t.OnOrderQty, t.UnitId, string.Empty);

    public ProductVendorDto WithNames(IReadOnlyDictionary<Guid, string> productNames, IReadOnlyDictionary<Guid, string> unitMeasureNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty), UnitName = unitMeasureNames.GetValueOrDefault(UnitId, string.Empty) };
}
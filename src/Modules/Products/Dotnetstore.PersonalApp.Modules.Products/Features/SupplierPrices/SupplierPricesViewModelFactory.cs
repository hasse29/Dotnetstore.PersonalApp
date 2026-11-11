using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;

/// <summary>Builds the suppliers card for one product; keeps the handler dependencies out of the product view models.</summary>
internal sealed class SupplierPricesViewModelFactory(
    ListSupplierPrices.Handler list,
    SetSupplierPrice.Handler set,
    DeleteSupplierPrice.Handler delete,
    ListSuppliers.Handler suppliers,
    ILocalizationService localization)
{
    public SupplierPricesViewModel Create(Guid productId, string unitLabel) =>
        new(productId, unitLabel, list, set, delete, suppliers, localization);
}

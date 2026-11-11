using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>Read-only page with everything about one product and its photo.</summary>
internal sealed partial class ProductDetailViewModel : ViewModelBase
{
    private readonly ILocalizationService _localization;

    public ProductDetailViewModel(
        ProductDto product,
        ILocalizationService localization,
        Func<string, PhotoPanelViewModel?>? photoFactory = null,
        SupplierPricesViewModel? supplierPrices = null,
        ProductStockViewModel? stock = null)
    {
        _localization = localization;
        Product = product;
        Photo = photoFactory?.Invoke(PhotoInitials.From(product.Name));
        SupplierPriceList = supplierPrices;
        StockCard = stock;
    }

    public ProductDto Product { get; }

    /// <summary>Each supplier's price for the product, with add, change and remove.</summary>
    public SupplierPricesViewModel? SupplierPriceList { get; }

    /// <summary>How many are in stock and where; null for products that cannot be stocked.</summary>
    public ProductStockViewModel? StockCard { get; }

    /// <summary>The photo with its Change and Remove buttons; null when the host has no file dialog.</summary>
    public PhotoPanelViewModel? Photo { get; }

    public bool IsHardware => Product.Kind == ProductKind.Hardware;

    public bool IsSoftware => Product.Kind == ProductKind.Software;

    public bool IsConsulting => Product.Kind == ProductKind.Consulting;

    public string KindLabel => _localization.Get($"ProductKind_{Product.Kind}");

    public string StatusText => _localization.Get(Product.IsActive ? "ProductDetail_Active" : "ProductDetail_Inactive");

    public string CategoryText => Product.CategoryName ?? _localization.Get("Products_NoCategory");

    public string PriceText => PerUnit(Product.UnitPrice);

    public string PriceWithVatText => PerUnit(Product.UnitPrice * (1 + (Product.VatRate / 100m)));

    public string CostText => PerUnit(Product.CostPrice);

    public string VatText => string.Create(CultureInfo.CurrentCulture, $"{Product.VatRate:0.##} %");

    /// <summary>Profit per unit and as a share of the price; empty when the product has no price.</summary>
    public string MarginText
    {
        get
        {
            if (Product.UnitPrice <= 0m)
            {
                return "-";
            }

            var profit = Product.UnitPrice - Product.CostPrice;
            var percent = profit / Product.UnitPrice * 100m;
            return string.Create(CultureInfo.CurrentCulture, $"{profit:N2} ({percent:0.#} %)");
        }
    }

    public string MinimumHoursText => string.Create(CultureInfo.CurrentCulture, $"{Product.MinimumHours:0.##}");

    public event EventHandler? Closed;

    public event EventHandler? EditRequested;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Edit() => EditRequested?.Invoke(this, EventArgs.Empty);

    private string PerUnit(decimal amount) =>
        string.Create(CultureInfo.CurrentCulture, $"{amount:N2} / {_localization.Get($"ProductUnit_{Product.Unit}")}");
}

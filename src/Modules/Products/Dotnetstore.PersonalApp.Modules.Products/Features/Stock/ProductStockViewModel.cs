using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>One location's line on the stock card.</summary>
internal sealed record ProductStockLine(string LocationName, string QuantityText);

/// <summary>The "Stock" card of a product's detail page: the total, the minimum and where the goods are.</summary>
internal sealed partial class ProductStockViewModel(
    Guid productId,
    string unitLabel,
    GetProductStock.Handler get,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<ProductStockLine> Locations { get; } = [];

    public bool IsEmpty => Locations.Count == 0;

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MinimumText { get; set; } = "-";

    [ObservableProperty]
    public partial string? WarningText { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var stock = await get.HandleAsync(new GetProductStock.Query(productId), cancellationToken);

        TotalText = string.Create(CultureInfo.CurrentCulture, $"{stock.Total:0.##} {unitLabel}");
        MinimumText = stock.MinimumStock is { } minimum ? string.Create(CultureInfo.CurrentCulture, $"{minimum:0.##} {unitLabel}") : "-";
        WarningText = stock.IsLow ? localization.Get("ProductStock_Low") : null;

        Locations.Clear();
        foreach (var location in stock.Locations)
        {
            Locations.Add(new ProductStockLine(location.LocationName, string.Create(CultureInfo.CurrentCulture, $"{location.Quantity:0.##} {unitLabel}")));
        }

        OnPropertyChanged(nameof(IsEmpty));
    }
}

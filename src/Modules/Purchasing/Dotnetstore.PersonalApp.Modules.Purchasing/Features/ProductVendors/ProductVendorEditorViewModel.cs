using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;

internal sealed partial class ProductVendorEditorViewModel : ViewModelBase
{
    private readonly ProductVendorDto? _existing;
    private readonly CreateProductVendor.Handler _create;
    private readonly UpdateProductVendor.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductVendorEditorViewModel(
        ProductVendorDto? existing,
        CreateProductVendor.Handler create,
        UpdateProductVendor.Handler update,
        IReadOnlyList<OptionDto> vendorOptions,
        IReadOnlyList<OptionDto> productOptions,
        IReadOnlyList<OptionDto> unitMeasureOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ProductOptions = productOptions;
        VendorOptions = vendorOptions;
        UnitOptions = unitMeasureOptions;

        if (existing is not null)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            SelectedVendor = VendorOptions.FirstOrDefault(o => o.Id == existing.VendorId);
            AverageLeadTime = existing.AverageLeadTime;
            StandardPrice = existing.StandardPrice;
            LastReceiptCost = existing.LastReceiptCost;
            LastReceiptDate = existing.LastReceiptDate?.ToDateTime(TimeOnly.MinValue);
            MinOrderQty = existing.MinOrderQty;
            MaxOrderQty = existing.MaxOrderQty;
            OnOrderQty = existing.OnOrderQty;
            SelectedUnit = UnitOptions.FirstOrDefault(o => o.Id == existing.UnitId);
        }
        else
        {
            // Every field starts empty.
        }

        if (fixedParentId is { } parentId)
        {
            SelectedVendor = VendorOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedVendor;
            IsParentFixed = true;
        }
    }

    public event EventHandler<ProductVendorDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductVendorEditor_New" : "ProductVendorEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    public IReadOnlyList<OptionDto> VendorOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedVendor { get; set; }

    [ObservableProperty]
    public partial decimal? AverageLeadTime { get; set; }

    [ObservableProperty]
    public partial decimal? StandardPrice { get; set; }

    [ObservableProperty]
    public partial decimal? LastReceiptCost { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? LastReceiptDate { get; set; }

    [ObservableProperty]
    public partial decimal? MinOrderQty { get; set; }

    [ObservableProperty]
    public partial decimal? MaxOrderQty { get; set; }

    [ObservableProperty]
    public partial decimal? OnOrderQty { get; set; }

    public IReadOnlyList<OptionDto> UnitOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductVendor.Command(SelectedProduct?.Id ?? Guid.Empty, SelectedVendor?.Id ?? Guid.Empty, (int)(AverageLeadTime ?? 0), StandardPrice ?? 0m, LastReceiptCost, ToDate(LastReceiptDate), (int)(MinOrderQty ?? 0), (int)(MaxOrderQty ?? 0), (int?)OnOrderQty, SelectedUnit?.Id ?? Guid.Empty), cancellationToken)
            : await _update.HandleAsync(new UpdateProductVendor.Command(existing.Id, SelectedProduct?.Id ?? Guid.Empty, SelectedVendor?.Id ?? Guid.Empty, (int)(AverageLeadTime ?? 0), StandardPrice ?? 0m, LastReceiptCost, ToDate(LastReceiptDate), (int)(MinOrderQty ?? 0), (int)(MaxOrderQty ?? 0), (int?)OnOrderQty, SelectedUnit?.Id ?? Guid.Empty), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private static DateOnly? ToDate(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}
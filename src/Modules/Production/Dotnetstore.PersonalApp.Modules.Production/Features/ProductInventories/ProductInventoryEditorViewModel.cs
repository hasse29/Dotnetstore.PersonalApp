using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;

internal sealed partial class ProductInventoryEditorViewModel : ViewModelBase
{
    private readonly ProductInventoryDto? _existing;
    private readonly CreateProductInventory.Handler _create;
    private readonly UpdateProductInventory.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductInventoryEditorViewModel(
        ProductInventoryDto? existing,
        CreateProductInventory.Handler create,
        UpdateProductInventory.Handler update,
        IReadOnlyList<OptionDto> productOptions,
        IReadOnlyList<OptionDto> locationOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ProductOptions = productOptions;
        LocationOptions = locationOptions;

        if (existing is not null)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == existing.LocationId);
            Shelf = existing.Shelf;
            Bin = existing.Bin;
            Quantity = existing.Quantity;
        }
        else
        {
            // Every field starts empty.
        }

        if (fixedParentId is { } parentId)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedProduct;
            IsParentFixed = true;
        }
    }

    public event EventHandler<ProductInventoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductInventoryEditor_New" : "ProductInventoryEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    public IReadOnlyList<OptionDto> LocationOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial string Shelf { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? Bin { get; set; }

    [ObservableProperty]
    public partial decimal? Quantity { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductInventory.Command(SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, Shelf, (int)(Bin ?? 0), (int)(Quantity ?? 0)), cancellationToken)
            : await _update.HandleAsync(new UpdateProductInventory.Command(existing.Id, SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, Shelf, (int)(Bin ?? 0), (int)(Quantity ?? 0)), cancellationToken);

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

}
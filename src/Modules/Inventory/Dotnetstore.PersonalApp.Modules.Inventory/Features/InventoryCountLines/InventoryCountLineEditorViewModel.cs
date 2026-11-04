using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;

internal sealed partial class InventoryCountLineEditorViewModel : ViewModelBase
{
    private readonly InventoryCountLineDto? _existing;
    private readonly CreateInventoryCountLine.Handler _create;
    private readonly UpdateInventoryCountLine.Handler _update;
    private readonly ILocalizationService _localization;

    public InventoryCountLineEditorViewModel(
        InventoryCountLineDto? existing,
        CreateInventoryCountLine.Handler create,
        UpdateInventoryCountLine.Handler update,
        IReadOnlyList<OptionDto> inventoryCountOptions,
        IReadOnlyList<OptionDto> storageLocationOptions,
        IReadOnlyList<OptionDto> productOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        InventoryCountOptions = inventoryCountOptions;
        ProductOptions = productOptions;
        LocationOptions = storageLocationOptions;

        if (existing is not null)
        {
            SelectedInventoryCount = InventoryCountOptions.FirstOrDefault(o => o.Id == existing.InventoryCountId);
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == existing.LocationId);
            ExpectedQty = existing.ExpectedQty;
            CountedQty = existing.CountedQty;
        }
        else
        {
            // Every field starts empty.
        }

        if (fixedParentId is { } parentId)
        {
            SelectedInventoryCount = InventoryCountOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedInventoryCount;
            IsParentFixed = true;
        }
    }

    public event EventHandler<InventoryCountLineDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "InventoryCountLineEditor_New" : "InventoryCountLineEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> InventoryCountOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedInventoryCount { get; set; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    public IReadOnlyList<OptionDto> LocationOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial decimal? ExpectedQty { get; set; }

    [ObservableProperty]
    public partial decimal? CountedQty { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateInventoryCountLine.Command(SelectedInventoryCount?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, ExpectedQty ?? 0m, CountedQty), cancellationToken)
            : await _update.HandleAsync(new UpdateInventoryCountLine.Command(existing.Id, SelectedInventoryCount?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, ExpectedQty ?? 0m, CountedQty), cancellationToken);

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
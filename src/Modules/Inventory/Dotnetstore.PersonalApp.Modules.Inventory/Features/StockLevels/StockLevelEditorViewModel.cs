using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;

internal sealed partial class StockLevelEditorViewModel : ViewModelBase
{
    private readonly StockLevelDto? _existing;
    private readonly CreateStockLevel.Handler _create;
    private readonly UpdateStockLevel.Handler _update;
    private readonly ILocalizationService _localization;

    public StockLevelEditorViewModel(
        StockLevelDto? existing,
        CreateStockLevel.Handler create,
        UpdateStockLevel.Handler update,
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

        ProductOptions = productOptions;
        LocationOptions = storageLocationOptions;

        if (existing is not null)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == existing.LocationId);
            MinQuantity = existing.MinQuantity;
        }
        else
        {
            // Every field starts empty.
        }

        if (fixedParentId is { } parentId)
        {
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedLocation;
            IsParentFixed = true;
        }
    }

    public event EventHandler<StockLevelDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "StockLevelEditor_New" : "StockLevelEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    public IReadOnlyList<OptionDto> LocationOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial decimal? MinQuantity { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateStockLevel.Command(SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, MinQuantity), cancellationToken)
            : await _update.HandleAsync(new UpdateStockLevel.Command(existing.Id, SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, MinQuantity), cancellationToken);

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
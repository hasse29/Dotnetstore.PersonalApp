using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

internal sealed partial class StockMovementEditorViewModel : ViewModelBase
{
    private readonly StockMovementDto? _existing;
    private readonly CreateStockMovement.Handler _create;
    private readonly UpdateStockMovement.Handler _update;
    private readonly ILocalizationService _localization;

    public StockMovementEditorViewModel(
        StockMovementDto? existing,
        CreateStockMovement.Handler create,
        UpdateStockMovement.Handler update,
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
        TypeOptions =
        [
            new ChoiceOption("Receipt", localization.Get("StockMovementChoice_Type_Receipt")),
            new ChoiceOption("Return", localization.Get("StockMovementChoice_Type_Return")),
            new ChoiceOption("TransferIn", localization.Get("StockMovementChoice_Type_TransferIn")),
            new ChoiceOption("AdjustmentUp", localization.Get("StockMovementChoice_Type_AdjustmentUp")),
            new ChoiceOption("Issue", localization.Get("StockMovementChoice_Type_Issue")),
            new ChoiceOption("Scrap", localization.Get("StockMovementChoice_Type_Scrap")),
            new ChoiceOption("TransferOut", localization.Get("StockMovementChoice_Type_TransferOut")),
            new ChoiceOption("AdjustmentDown", localization.Get("StockMovementChoice_Type_AdjustmentDown")),
        ];

        if (existing is not null)
        {
            Date = existing.Date.ToDateTime(TimeOnly.MinValue);
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == existing.LocationId);
            SelectedType = TypeOptions.FirstOrDefault(o => o.Code == existing.Type);
            Quantity = existing.Quantity;
            Reference = existing.Reference ?? string.Empty;
            Note = existing.Note ?? string.Empty;
        }
        else
        {
            Date = clock.GetLocalNow().Date;
            SelectedType = TypeOptions[0];
        }

        if (fixedParentId is { } parentId)
        {
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedLocation;
            IsParentFixed = true;
        }
    }

    public event EventHandler<StockMovementDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "StockMovementEditor_New" : "StockMovementEditor_Edit");

    public bool IsParentFixed { get; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    public IReadOnlyList<OptionDto> LocationOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedLocation { get; set; }

    public IReadOnlyList<ChoiceOption> TypeOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedType { get; set; }

    [ObservableProperty]
    public partial decimal? Quantity { get; set; }

    [ObservableProperty]
    public partial string Reference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateStockMovement.Command(DateOnly.FromDateTime(Date ?? DateTime.MinValue), SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, SelectedType?.Code ?? string.Empty, Quantity ?? 0m, Reference, Note), cancellationToken)
            : await _update.HandleAsync(new UpdateStockMovement.Command(existing.Id, DateOnly.FromDateTime(Date ?? DateTime.MinValue), SelectedProduct?.Id ?? Guid.Empty, SelectedLocation?.Id ?? Guid.Empty, SelectedType?.Code ?? string.Empty, Quantity ?? 0m, Reference, Note), cancellationToken);

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
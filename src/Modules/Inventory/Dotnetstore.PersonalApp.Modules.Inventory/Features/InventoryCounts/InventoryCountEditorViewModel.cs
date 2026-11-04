using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

internal sealed partial class InventoryCountEditorViewModel : ViewModelBase
{
    private readonly InventoryCountDto? _existing;
    private readonly CreateInventoryCount.Handler _create;
    private readonly UpdateInventoryCount.Handler _update;
    private readonly ILocalizationService _localization;

    public InventoryCountEditorViewModel(
        InventoryCountDto? existing,
        CreateInventoryCount.Handler create,
        UpdateInventoryCount.Handler update,
        IReadOnlyList<OptionDto> warehouseOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        WarehouseOptions = warehouseOptions;

        if (existing is not null)
        {
            Number = existing.Number;
            SelectedWarehouse = WarehouseOptions.FirstOrDefault(o => o.Id == existing.WarehouseId);
            Date = existing.Date.ToDateTime(TimeOnly.MinValue);
            Note = existing.Note ?? string.Empty;
        }
        else
        {
            Date = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedWarehouse = WarehouseOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedWarehouse;
            IsParentFixed = true;
        }
    }

    public event EventHandler<InventoryCountDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "InventoryCountEditor_New" : "InventoryCountEditor_Edit");

    public bool IsParentFixed { get; }

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> WarehouseOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedWarehouse { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateInventoryCount.Command(Number, SelectedWarehouse?.Id ?? Guid.Empty, DateOnly.FromDateTime(Date ?? DateTime.MinValue), Note), cancellationToken)
            : await _update.HandleAsync(new UpdateInventoryCount.Command(existing.Id, Number, SelectedWarehouse?.Id ?? Guid.Empty, DateOnly.FromDateTime(Date ?? DateTime.MinValue), Note), cancellationToken);

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
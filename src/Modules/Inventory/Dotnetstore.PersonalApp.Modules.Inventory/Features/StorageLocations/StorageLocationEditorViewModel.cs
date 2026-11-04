using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

internal sealed partial class StorageLocationEditorViewModel : ViewModelBase
{
    private readonly StorageLocationDto? _existing;
    private readonly CreateStorageLocation.Handler _create;
    private readonly UpdateStorageLocation.Handler _update;
    private readonly ILocalizationService _localization;

    public StorageLocationEditorViewModel(
        StorageLocationDto? existing,
        CreateStorageLocation.Handler create,
        UpdateStorageLocation.Handler update,
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
            SelectedWarehouse = WarehouseOptions.FirstOrDefault(o => o.Id == existing.WarehouseId);
            Code = existing.Code;
            Zone = existing.Zone ?? string.Empty;
            Description = existing.Description ?? string.Empty;
            Capacity = existing.Capacity;
        }
        else
        {
            // Every field starts empty.
        }

        if (fixedParentId is { } parentId)
        {
            SelectedWarehouse = WarehouseOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedWarehouse;
            IsParentFixed = true;
        }
    }

    public event EventHandler<StorageLocationDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "StorageLocationEditor_New" : "StorageLocationEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> WarehouseOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedWarehouse { get; set; }

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Zone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? Capacity { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateStorageLocation.Command(SelectedWarehouse?.Id ?? Guid.Empty, Code, Zone, Description, Capacity), cancellationToken)
            : await _update.HandleAsync(new UpdateStorageLocation.Command(existing.Id, SelectedWarehouse?.Id ?? Guid.Empty, Code, Zone, Description, Capacity), cancellationToken);

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
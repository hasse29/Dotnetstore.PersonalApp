using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;

internal sealed partial class WarehouseEditorViewModel : ViewModelBase
{
    private readonly WarehouseDto? _existing;
    private readonly CreateWarehouse.Handler _create;
    private readonly UpdateWarehouse.Handler _update;
    private readonly ILocalizationService _localization;

    public WarehouseEditorViewModel(
        WarehouseDto? existing,
        CreateWarehouse.Handler create,
        UpdateWarehouse.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Code = existing.Code;
            Name = existing.Name;
            Address = existing.Address ?? string.Empty;
            Active = existing.Active;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<WarehouseDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "WarehouseEditor_New" : "WarehouseEditor_Edit");

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Active { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateWarehouse.Command(Code, Name, Address, Active), cancellationToken)
            : await _update.HandleAsync(new UpdateWarehouse.Command(existing.Id, Code, Name, Address, Active), cancellationToken);

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
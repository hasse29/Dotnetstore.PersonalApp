using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;

internal sealed partial class ShipMethodEditorViewModel : ViewModelBase
{
    private readonly ShipMethodDto? _existing;
    private readonly CreateShipMethod.Handler _create;
    private readonly UpdateShipMethod.Handler _update;
    private readonly ILocalizationService _localization;

    public ShipMethodEditorViewModel(
        ShipMethodDto? existing,
        CreateShipMethod.Handler create,
        UpdateShipMethod.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Name = existing.Name;
            ShipBase = existing.ShipBase;
            ShipRate = existing.ShipRate;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<ShipMethodDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ShipMethodEditor_New" : "ShipMethodEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? ShipBase { get; set; }

    [ObservableProperty]
    public partial decimal? ShipRate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateShipMethod.Command(Name, ShipBase ?? 0m, ShipRate ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateShipMethod.Command(existing.Id, Name, ShipBase ?? 0m, ShipRate ?? 0m), cancellationToken);

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
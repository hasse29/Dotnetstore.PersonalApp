using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Locations;

internal sealed partial class LocationEditorViewModel : ViewModelBase
{
    private readonly LocationDto? _existing;
    private readonly CreateLocation.Handler _create;
    private readonly UpdateLocation.Handler _update;
    private readonly ILocalizationService _localization;

    public LocationEditorViewModel(
        LocationDto? existing,
        CreateLocation.Handler create,
        UpdateLocation.Handler update,
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
            CostRate = existing.CostRate;
            Availability = existing.Availability;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<LocationDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "LocationEditor_New" : "LocationEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? CostRate { get; set; }

    [ObservableProperty]
    public partial decimal? Availability { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateLocation.Command(Name, CostRate ?? 0m, Availability ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateLocation.Command(existing.Id, Name, CostRate ?? 0m, Availability ?? 0m), cancellationToken);

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
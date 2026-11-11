using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

/// <param name="Id">Null for "all locations".</param>
internal sealed record CountLocationOption(Guid? Id, string Name);

/// <summary>The form that starts a new inventory count.</summary>
internal sealed partial class InventoryCountEditorViewModel : ViewModelBase
{
    private readonly CreateInventoryCount.Handler _create;
    private readonly ILocalizationService _localization;

    public InventoryCountEditorViewModel(
        IReadOnlyList<CountLocationOption> locations,
        CreateInventoryCount.Handler create,
        ILocalizationService localization,
        DateTimeOffset now)
    {
        _create = create;
        _localization = localization;
        Locations = locations;
        SelectedLocation = locations[0];
        Name = localization.Format("InventoryCountEditor_DefaultName", now.ToString("d", System.Globalization.CultureInfo.CurrentCulture));
    }

    public event EventHandler<InventoryCountDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<CountLocationOption> Locations { get; }

    /// <summary>Products without stock can only be listed for one location, so the box is only offered with a location chosen.</summary>
    public bool CanIncludeUnstocked => SelectedLocation?.Id is not null;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIncludeUnstocked))]
    public partial CountLocationOption? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IncludeUnstocked { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    partial void OnSelectedLocationChanged(CountLocationOption? value)
    {
        if (value?.Id is null)
        {
            IncludeUnstocked = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = await _create.HandleAsync(
            new CreateInventoryCount.Command(Name, SelectedLocation?.Id, Note, IncludeUnstocked), cancellationToken);

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

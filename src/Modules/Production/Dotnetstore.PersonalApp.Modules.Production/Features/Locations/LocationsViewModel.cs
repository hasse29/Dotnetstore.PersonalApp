using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Locations;

internal sealed partial class LocationsViewModel(
    ListLocations.Handler list,
    CreateLocation.Handler create,
    UpdateLocation.Handler update,
    DeleteLocation.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<LocationDto> Locations { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial LocationDto? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial LocationEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedLocationChanged(LocationDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new LocationEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListLocations.Query(Search), cancellationToken);

        Locations.Clear();
        foreach (var item in response.Items)
        {
            Locations.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Locations_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Locations_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedLocation = null;
        OpenEditor(new LocationEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedLocation is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteLocation.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedLocation is not null;

    private void OpenEditor(LocationEditorViewModel editor)
    {
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedLocation = null;
        IsConfirmingDelete = false;
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnEditorSaved(object? sender, LocationDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("Locations_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
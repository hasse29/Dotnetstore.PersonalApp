using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

/// <summary>A location as the list shows it: the DTO plus its status in the current UI language.</summary>
internal sealed record StorageLocationRow(StorageLocationDto Location, string StatusText);

internal sealed partial class StorageLocationsViewModel(
    ListStorageLocations.Handler list,
    CreateStorageLocation.Handler create,
    UpdateStorageLocation.Handler update,
    DeleteStorageLocation.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<StorageLocationRow> Locations { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial StorageLocationRow? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial StorageLocationEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedLocationChanged(StorageLocationRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new StorageLocationEditorViewModel(value.Location, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListStorageLocations.Query(Search), cancellationToken);

        Locations.Clear();
        foreach (var location in response.Items)
        {
            Locations.Add(new StorageLocationRow(location, localization.Get(location.IsActive ? "StorageLocations_Active" : "StorageLocations_Inactive")));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("StorageLocations_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("StorageLocations_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedLocation = null;
        OpenEditor(new StorageLocationEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedLocation is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteStorageLocation.Command(row.Location.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedLocation is not null;

    private void OpenEditor(StorageLocationEditorViewModel editor)
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
    private async void OnEditorSaved(object? sender, StorageLocationDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("StorageLocations_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

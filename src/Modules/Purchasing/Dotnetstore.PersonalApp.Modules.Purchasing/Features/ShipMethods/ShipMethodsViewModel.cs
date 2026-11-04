using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;

internal sealed partial class ShipMethodsViewModel(
    ListShipMethods.Handler list,
    CreateShipMethod.Handler create,
    UpdateShipMethod.Handler update,
    DeleteShipMethod.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<ShipMethodDto> ShipMethods { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ShipMethodDto? SelectedShipMethod { get; set; }

    [ObservableProperty]
    public partial ShipMethodEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedShipMethodChanged(ShipMethodDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ShipMethodEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListShipMethods.Query(Search), cancellationToken);

        ShipMethods.Clear();
        foreach (var item in response.Items)
        {
            ShipMethods.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ShipMethods_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ShipMethods_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedShipMethod = null;
        OpenEditor(new ShipMethodEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedShipMethod is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteShipMethod.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedShipMethod is not null;

    private void OpenEditor(ShipMethodEditorViewModel editor)
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
        SelectedShipMethod = null;
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
    private async void OnEditorSaved(object? sender, ShipMethodDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ShipMethods_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
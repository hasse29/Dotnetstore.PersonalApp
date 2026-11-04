using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.Locations;
using Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;

internal sealed partial class WorkOrderRoutingsViewModel(
    ListWorkOrderRoutings.Handler list,
    CreateWorkOrderRouting.Handler create,
    UpdateWorkOrderRouting.Handler update,
    DeleteWorkOrderRouting.Handler delete,
    ListWorkOrderOptions.Handler listWorkOrderOptions,
    ListLocationOptions.Handler listLocationOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _workOrderOptions = [];
    private IReadOnlyList<OptionDto> _locationOptions = [];

    public ObservableCollection<WorkOrderRoutingDto> WorkOrderRoutings { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial WorkOrderRoutingDto? SelectedWorkOrderRouting { get; set; }

    [ObservableProperty]
    public partial WorkOrderRoutingEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedWorkOrderRoutingChanged(WorkOrderRoutingDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new WorkOrderRoutingEditorViewModel(value, create, update, _workOrderOptions, _locationOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _workOrderOptions = await listWorkOrderOptions.HandleAsync(cancellationToken);
        _locationOptions = await listLocationOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListWorkOrderRoutings.Query(Search), cancellationToken);

        WorkOrderRoutings.Clear();
        foreach (var item in response.Items)
        {
            WorkOrderRoutings.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("WorkOrderRoutings_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("WorkOrderRoutings_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedWorkOrderRouting = null;
        OpenEditor(new WorkOrderRoutingEditorViewModel(null, create, update, _workOrderOptions, _locationOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedWorkOrderRouting is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteWorkOrderRouting.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedWorkOrderRouting is not null;

    private void OpenEditor(WorkOrderRoutingEditorViewModel editor)
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
        SelectedWorkOrderRouting = null;
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
    private async void OnEditorSaved(object? sender, WorkOrderRoutingDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("WorkOrderRoutings_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
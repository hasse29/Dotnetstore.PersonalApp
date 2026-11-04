using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal sealed partial class WorkOrdersViewModel(
    ListWorkOrders.Handler list,
    CreateWorkOrder.Handler create,
    UpdateWorkOrder.Handler update,
    DeleteWorkOrder.Handler delete,
    ListProductOptions.Handler listProductOptions,
    ListScrapReasonOptions.Handler listScrapReasonOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _productOptions = [];
    private IReadOnlyList<OptionDto> _scrapReasonOptions = [];

    public ObservableCollection<WorkOrderDto> WorkOrders { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial WorkOrderDto? SelectedWorkOrder { get; set; }

    [ObservableProperty]
    public partial WorkOrderEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedWorkOrderChanged(WorkOrderDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new WorkOrderEditorViewModel(value, create, update, _productOptions, _scrapReasonOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _productOptions = await listProductOptions.HandleAsync(cancellationToken);
        _scrapReasonOptions = await listScrapReasonOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListWorkOrders.Query(Search), cancellationToken);

        WorkOrders.Clear();
        foreach (var item in response.Items)
        {
            WorkOrders.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("WorkOrders_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("WorkOrders_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedWorkOrder = null;
        OpenEditor(new WorkOrderEditorViewModel(null, create, update, _productOptions, _scrapReasonOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedWorkOrder is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteWorkOrder.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedWorkOrder is not null;

    private void OpenEditor(WorkOrderEditorViewModel editor)
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
        SelectedWorkOrder = null;
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
    private async void OnEditorSaved(object? sender, WorkOrderDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("WorkOrders_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;

internal sealed partial class SalesOrderReasonsViewModel(
    ListSalesOrderReasons.Handler list,
    CreateSalesOrderReason.Handler create,
    UpdateSalesOrderReason.Handler update,
    DeleteSalesOrderReason.Handler delete,
    ListSalesOrderOptions.Handler listSalesOrderOptions,
    ListSalesReasonOptions.Handler listSalesReasonOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _salesOrderOptions = [];
    private IReadOnlyList<OptionDto> _salesReasonOptions = [];

    public ObservableCollection<SalesOrderReasonDto> SalesOrderReasons { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesOrderReasonDto? SelectedSalesOrderReason { get; set; }

    [ObservableProperty]
    public partial SalesOrderReasonEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesOrderReasonChanged(SalesOrderReasonDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesOrderReasonEditorViewModel(value, create, update, _salesOrderOptions, _salesReasonOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _salesOrderOptions = await listSalesOrderOptions.HandleAsync(cancellationToken);
        _salesReasonOptions = await listSalesReasonOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSalesOrderReasons.Query(Search), cancellationToken);

        SalesOrderReasons.Clear();
        foreach (var item in response.Items)
        {
            SalesOrderReasons.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesOrderReasons_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesOrderReasons_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesOrderReason = null;
        OpenEditor(new SalesOrderReasonEditorViewModel(null, create, update, _salesOrderOptions, _salesReasonOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesOrderReason is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesOrderReason.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSalesOrderReason is not null;

    private void OpenEditor(SalesOrderReasonEditorViewModel editor)
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
        SelectedSalesOrderReason = null;
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
    private async void OnEditorSaved(object? sender, SalesOrderReasonDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesOrderReasons_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;

internal sealed partial class SalesPersonQuotaHistoriesViewModel(
    ListSalesPersonQuotaHistories.Handler list,
    CreateSalesPersonQuotaHistory.Handler create,
    UpdateSalesPersonQuotaHistory.Handler update,
    DeleteSalesPersonQuotaHistory.Handler delete,
    ListSalesPersonOptions.Handler listSalesPersonOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _salesPersonOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public SalesPersonQuotaHistoriesViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<SalesPersonQuotaHistoryDto> SalesPersonQuotaHistories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesPersonQuotaHistoryDto? SelectedSalesPersonQuotaHistory { get; set; }

    [ObservableProperty]
    public partial SalesPersonQuotaHistoryEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesPersonQuotaHistoryChanged(SalesPersonQuotaHistoryDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesPersonQuotaHistoryEditorViewModel(value, create, update, _salesPersonOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _salesPersonOptions = await listSalesPersonOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSalesPersonQuotaHistories.Query(Search, ParentId: _parentId), cancellationToken);

        SalesPersonQuotaHistories.Clear();
        foreach (var item in response.Items)
        {
            SalesPersonQuotaHistories.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesPersonQuotaHistories_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesPersonQuotaHistories_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesPersonQuotaHistory = null;
        OpenEditor(new SalesPersonQuotaHistoryEditorViewModel(null, create, update, _salesPersonOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesPersonQuotaHistory is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesPersonQuotaHistory.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
        else
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool HasSelection() => SelectedSalesPersonQuotaHistory is not null;

    private void OpenEditor(SalesPersonQuotaHistoryEditorViewModel editor)
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
        SelectedSalesPersonQuotaHistory = null;
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
    private async void OnEditorSaved(object? sender, SalesPersonQuotaHistoryDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesPersonQuotaHistories_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
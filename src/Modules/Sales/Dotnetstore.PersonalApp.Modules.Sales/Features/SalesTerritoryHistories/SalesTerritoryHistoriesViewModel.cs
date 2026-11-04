using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;

internal sealed partial class SalesTerritoryHistoriesViewModel(
    ListSalesTerritoryHistories.Handler list,
    CreateSalesTerritoryHistory.Handler create,
    UpdateSalesTerritoryHistory.Handler update,
    DeleteSalesTerritoryHistory.Handler delete,
    ListSalesPersonOptions.Handler listSalesPersonOptions,
    ListSalesTerritoryOptions.Handler listSalesTerritoryOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _salesPersonOptions = [];
    private IReadOnlyList<OptionDto> _salesTerritoryOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public SalesTerritoryHistoriesViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<SalesTerritoryHistoryDto> SalesTerritoryHistories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesTerritoryHistoryDto? SelectedSalesTerritoryHistory { get; set; }

    [ObservableProperty]
    public partial SalesTerritoryHistoryEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesTerritoryHistoryChanged(SalesTerritoryHistoryDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesTerritoryHistoryEditorViewModel(value, create, update, _salesPersonOptions, _salesTerritoryOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _salesPersonOptions = await listSalesPersonOptions.HandleAsync(cancellationToken);
        _salesTerritoryOptions = await listSalesTerritoryOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSalesTerritoryHistories.Query(Search, ParentId: _parentId), cancellationToken);

        SalesTerritoryHistories.Clear();
        foreach (var item in response.Items)
        {
            SalesTerritoryHistories.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesTerritoryHistories_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesTerritoryHistories_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesTerritoryHistory = null;
        OpenEditor(new SalesTerritoryHistoryEditorViewModel(null, create, update, _salesPersonOptions, _salesTerritoryOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesTerritoryHistory is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesTerritoryHistory.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedSalesTerritoryHistory is not null;

    private void OpenEditor(SalesTerritoryHistoryEditorViewModel editor)
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
        SelectedSalesTerritoryHistory = null;
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
    private async void OnEditorSaved(object? sender, SalesTerritoryHistoryDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesTerritoryHistories_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
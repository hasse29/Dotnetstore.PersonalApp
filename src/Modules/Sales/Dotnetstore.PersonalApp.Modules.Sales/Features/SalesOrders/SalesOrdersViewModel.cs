using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal sealed partial class SalesOrdersViewModel(
    ListSalesOrders.Handler list,
    CreateSalesOrder.Handler create,
    UpdateSalesOrder.Handler update,
    DeleteSalesOrder.Handler delete,
    IDetailFactory<SalesOrderDto> detailFactory,
    ListCustomerOptions.Handler listCustomerOptions,
    ListSalesPersonOptions.Handler listSalesPersonOptions,
    ListSalesTerritoryOptions.Handler listSalesTerritoryOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _customerOptions = [];
    private IReadOnlyList<OptionDto> _salesPersonOptions = [];
    private IReadOnlyList<OptionDto> _salesTerritoryOptions = [];
    private IReadOnlyList<OptionDto> _shipMethodOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public SalesOrdersViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedSalesOrder is not { } selected)
        {
            return;
        }

        var detail = detailFactory.Create(selected);
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public ObservableCollection<SalesOrderDto> SalesOrders { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SalesOrderDto? SelectedSalesOrder { get; set; }

    [ObservableProperty]
    public partial SalesOrderEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSalesOrderChanged(SalesOrderDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SalesOrderEditorViewModel(value, create, update, _customerOptions, _salesPersonOptions, _salesTerritoryOptions, _shipMethodOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _customerOptions = await listCustomerOptions.HandleAsync(cancellationToken);
        _salesPersonOptions = await listSalesPersonOptions.HandleAsync(cancellationToken);
        _salesTerritoryOptions = await listSalesTerritoryOptions.HandleAsync(cancellationToken);
        _shipMethodOptions = await references.ShipMethodOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListSalesOrders.Query(Search, ParentId: _parentId), cancellationToken);

        SalesOrders.Clear();
        foreach (var item in response.Items)
        {
            SalesOrders.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("SalesOrders_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("SalesOrders_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSalesOrder = null;
        OpenEditor(new SalesOrderEditorViewModel(null, create, update, _customerOptions, _salesPersonOptions, _salesTerritoryOptions, _shipMethodOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSalesOrder is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSalesOrder.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedSalesOrder is not null;

    private void OpenEditor(SalesOrderEditorViewModel editor)
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
        SelectedSalesOrder = null;
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
    private async void OnEditorSaved(object? sender, SalesOrderDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("SalesOrders_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
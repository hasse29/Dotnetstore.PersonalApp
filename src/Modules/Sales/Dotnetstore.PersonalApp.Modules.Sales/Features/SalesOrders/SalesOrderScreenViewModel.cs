using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

/// <summary>One sales order on one screen: the header on the left, its totals and lines on the right.</summary>
internal sealed partial class SalesOrderScreenViewModel : DetailViewModel
{
    private readonly ListSalesOrders.Handler _list;
    private readonly CreateSalesOrder.Handler _create;
    private readonly UpdateSalesOrder.Handler _update;
    private readonly ListCustomerOptions.Handler _listCustomerOptions;
    private readonly ListSalesPersonOptions.Handler _listSalesPersonOptions;
    private readonly ListSalesTerritoryOptions.Handler _listSalesTerritoryOptions;
    private readonly ExternalReferences _references;
    private readonly TimeProvider _clock;
    private readonly ILocalizationService _localization;

    private IReadOnlyList<OptionDto> _customerOptions = [];
    private IReadOnlyList<OptionDto> _salesPersonOptions = [];
    private IReadOnlyList<OptionDto> _salesTerritoryOptions = [];
    private IReadOnlyList<OptionDto> _shipMethodOptions = [];

    public SalesOrderScreenViewModel(
        SalesOrderDto order,
        ListSalesOrders.Handler list,
        CreateSalesOrder.Handler create,
        UpdateSalesOrder.Handler update,
        ListCustomerOptions.Handler listCustomerOptions,
        ListSalesPersonOptions.Handler listSalesPersonOptions,
        ListSalesTerritoryOptions.Handler listSalesTerritoryOptions,
        ExternalReferences references,
        SalesOrderDetailsViewModel lines,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _list = list;
        _create = create;
        _update = update;
        _listCustomerOptions = listCustomerOptions;
        _listSalesPersonOptions = listSalesPersonOptions;
        _listSalesTerritoryOptions = listSalesTerritoryOptions;
        _references = references;
        _clock = clock;
        _localization = localization;

        Order = order;
        Lines = lines.ForParent(order.Id);
        Lines.Changed += OnLinesChanged;
    }

    public SalesOrderDetailsViewModel Lines { get; }

    /// <summary>The order as last saved, with the totals the lines add up to.</summary>
    [ObservableProperty]
    public partial SalesOrderDto Order { get; set; }

    [ObservableProperty]
    public partial SalesOrderEditorViewModel? Editor { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _customerOptions = await _listCustomerOptions.HandleAsync(cancellationToken);
        _salesPersonOptions = await _listSalesPersonOptions.HandleAsync(cancellationToken);
        _salesTerritoryOptions = await _listSalesTerritoryOptions.HandleAsync(cancellationToken);
        _shipMethodOptions = await _references.ShipMethodOptionsAsync(cancellationToken);

        OpenEditor(Order);
        Lines.LoadCommand.Execute(null);
    }

    private void OpenEditor(SalesOrderDto order)
    {
        if (Editor is { } old)
        {
            old.Saved -= OnEditorSaved;
            old.Cancelled -= OnEditorCancelled;
        }

        var editor = new SalesOrderEditorViewModel(order, _create, _update, _customerOptions, _salesPersonOptions, _salesTerritoryOptions, _shipMethodOptions, _clock, _localization);
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private void OnEditorSaved(object? sender, SalesOrderDto saved)
    {
        Order = saved;
        OpenEditor(saved);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => OpenEditor(Order);

    // Event handler, so it has to be async void; a failed refresh leaves the totals as they were.
    private async void OnLinesChanged(object? sender, EventArgs e)
    {
        var response = await _list.HandleAsync(new ListSalesOrders.Query(Order.Number, ParentId: Order.CustomerId), CancellationToken.None);
        if (response.Items.FirstOrDefault(o => o.Id == Order.Id) is { } fresh)
        {
            Order = fresh;
        }
    }
}

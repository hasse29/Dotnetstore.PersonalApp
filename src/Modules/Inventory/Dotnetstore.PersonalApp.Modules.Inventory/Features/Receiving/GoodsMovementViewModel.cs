using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Orders;
using Dotnetstore.PersonalApp.Shared.Purchasing;
using Dotnetstore.PersonalApp.Shared.Sales;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;

/// <summary>One line of the order being received or issued, with how much has been handled and how much to handle now.</summary>
internal sealed partial class OrderLineRow(Guid lineId, Guid productId, string productName, decimal ordered, decimal handled) : ObservableObject
{
    public Guid LineId { get; } = lineId;

    public Guid ProductId { get; } = productId;

    public string ProductName { get; } = productName;

    public decimal Ordered { get; } = ordered;

    public decimal Handled { get; } = handled;

    public decimal Outstanding => Math.Max(Ordered - Handled, 0m);

    /// <summary>What is received or issued now; it starts at what is left on the line.</summary>
    [ObservableProperty]
    public partial decimal? Quantity { get; set; } = Math.Max(ordered - handled, 0m);
}

/// <summary>Pick an order and a location, then receive or issue its lines; the two pages differ only in direction.</summary>
internal abstract partial class GoodsMovementViewModel(
    IOrderLookup? orders,
    bool receiving,
    OrderMovements.Handler movements,
    ListStorageLocationOptions.Handler listLocations,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private OrderInfo? _order;
    private IReadOnlyList<OrderInfo> _orderInfos = [];

    /// <summary>False when the module that owns the orders is not installed.</summary>
    public bool HasOrders => orders is not null;

    public string Subtitle => localization.Get(receiving ? "GoodsReceipt_Subtitle" : "GoodsIssue_Subtitle");

    public string ConfirmText => localization.Get(receiving ? "GoodsReceipt_Confirm" : "GoodsIssue_Confirm");

    public string HandledText => localization.Get(receiving ? "GoodsReceipt_Handled" : "GoodsIssue_Handled");

    public ObservableCollection<OrderLineRow> Lines { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OptionDto> Orders { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OptionDto> Locations { get; set; } = [];

    [ObservableProperty]
    public partial OptionDto? SelectedOrder { get; set; }

    [ObservableProperty]
    public partial OptionDto? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    partial void OnSelectedOrderChanged(OptionDto? value) => LoadLinesCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Locations = await listLocations.HandleAsync(cancellationToken);
        if (orders is null)
        {
            return;
        }

        var all = await orders.ListAsync(cancellationToken);
        Orders = [.. all.Select(o => new OptionDto(o.Id, $"{o.Number} - {o.Party}"))];
        _orderInfos = all;
    }

    [RelayCommand]
    private async Task LoadLinesAsync(CancellationToken cancellationToken)
    {
        Lines.Clear();
        _order = SelectedOrder is { } selected ? _orderInfos.FirstOrDefault(o => o.Id == selected.Id) : null;
        if (orders is null || _order is null)
        {
            return;
        }

        var lines = await orders.ListLinesAsync(_order.Id, cancellationToken);
        var names = await references.ProductNamesAsync(cancellationToken);
        IReadOnlyList<OrderMovements.Line> asLines = [.. lines.Select(l => new OrderMovements.Line(l.LineId, l.ProductId, l.Quantity, 0m))];

        // Opening the order also resends what has been handled, so an order module that missed an earlier message catches up.
        await movements.ResyncAsync(_order.Number, receiving, asLines, cancellationToken);
        var handled = await movements.HandledAsync(_order.Number, receiving, asLines, cancellationToken);

        foreach (var line in lines)
        {
            Lines.Add(new OrderLineRow(line.LineId, line.ProductId, names.GetValueOrDefault(line.ProductId, string.Empty), line.Quantity, handled.GetValueOrDefault(line.LineId)));
        }
    }

    [RelayCommand]
    private async Task ConfirmAsync(CancellationToken cancellationToken)
    {
        StatusMessage = null;
        ErrorMessage = null;

        if (_order is null || SelectedLocation is null)
        {
            ErrorMessage = localization.Get("GoodsMovement_ChooseOrderAndLocation");
            return;
        }

        var command = new OrderMovements.Command(
            receiving,
            SelectedLocation.Id,
            _order.Number,
            DateOnly.FromDateTime(clock.GetLocalNow().DateTime),
            [.. Lines.Select(l => new OrderMovements.Line(l.LineId, l.ProductId, l.Ordered, l.Quantity ?? 0m))]);
        var result = await movements.RecordAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            StatusMessage = localization.Format(receiving ? "GoodsReceipt_Done" : "GoodsIssue_Done", result.Value);
            await LoadLinesAsync(cancellationToken);
        }
        else
        {
            ErrorMessage = localization.Describe(result.Error);
        }
    }
}

internal sealed class GoodsReceiptViewModel(
    IPurchaseOrderLookup? orders,
    OrderMovements.Handler movements,
    ListStorageLocationOptions.Handler listLocations,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization)
    : GoodsMovementViewModel(orders, true, movements, listLocations, references, clock, localization);

internal sealed class GoodsIssueViewModel(
    ISalesOrderLookup? orders,
    OrderMovements.Handler movements,
    ListStorageLocationOptions.Handler listLocations,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization)
    : GoodsMovementViewModel(orders, false, movements, listLocations, references, clock, localization);

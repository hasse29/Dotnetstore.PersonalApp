using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

/// <summary>An order as the list shows it: the DTO plus its texts in the current UI language.</summary>
internal sealed record OrderRow(OrderDto Order, string StatusText, string DateText, string TotalText, string PickText);

internal sealed record OrderStatusOption(OrderStatus? Status, string Label);

/// <summary>The orders list; opening an order replaces the list with the order screen.</summary>
internal sealed partial class OrdersViewModel : ViewModelBase
{
    private readonly ListOrders.Handler _list;
    private readonly GetOrder.Handler _get;
    private readonly CancelOrder.Handler _cancel;
    private readonly CreateInvoiceFromOrder.Handler _createInvoice;
    private readonly ILocalizationService _localization;
    private readonly bool _ready;

    public OrdersViewModel(ListOrders.Handler list, GetOrder.Handler get, CancelOrder.Handler cancel, CreateInvoiceFromOrder.Handler createInvoice, ILocalizationService localization)
    {
        _list = list;
        _get = get;
        _cancel = cancel;
        _createInvoice = createInvoice;
        _localization = localization;

        StatusOptions =
        [
            new OrderStatusOption(null, localization.Get("Orders_AllStatuses")),
            .. Enum.GetValues<OrderStatus>().Select(s => new OrderStatusOption(s, localization.Get($"OrderStatus_{s}"))),
        ];
        SelectedStatus = StatusOptions[0];
        _ready = true;
    }

    public IReadOnlyList<OrderStatusOption> StatusOptions { get; }

    public ObservableCollection<OrderRow> Orders { get; } = [];

    public bool HasScreen => Screen is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreen))]
    public partial OrderScreenViewModel? Screen { get; set; }

    [ObservableProperty]
    public partial OrderRow? SelectedOrder { get; set; }

    [ObservableProperty]
    public partial OrderStatusOption SelectedStatus { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedOrderChanged(OrderRow? value)
    {
        if (value is null)
        {
            return;
        }

        var screen = new OrderScreenViewModel(value.Order.Id, _get, _cancel, _createInvoice, _localization);
        screen.Closed += OnScreenClosed;
        Screen = screen;
        screen.LoadCommand.Execute(null);
    }

    partial void OnSelectedStatusChanged(OrderStatusOption value)
    {
        if (_ready)
        {
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await _list.HandleAsync(new ListOrders.Query(Search, SelectedStatus.Status), cancellationToken);

        Orders.Clear();
        foreach (var order in response.Items)
        {
            Orders.Add(new OrderRow(
                order,
                _localization.Get($"OrderStatus_{order.Status}"),
                order.Date.ToString("d", CultureInfo.CurrentCulture),
                order.Total.ToString("N2", CultureInfo.CurrentCulture),
                order.PickLines == 0
                    ? _localization.Get("Orders_NothingToPick")
                    : _localization.Format("Orders_PickProgress", order.PickedLines, order.PickLines)));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? _localization.Format("Orders_ShowingPartial", response.Items.Count, response.TotalCount)
            : _localization.Format("Orders_Count", response.TotalCount);
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnScreenClosed(object? sender, bool reload)
    {
        if (sender is OrderScreenViewModel screen)
        {
            screen.Closed -= OnScreenClosed;
        }

        Screen = null;
        SelectedOrder = null;
        if (reload)
        {
            await LoadAsync(CancellationToken.None);
        }
    }
}

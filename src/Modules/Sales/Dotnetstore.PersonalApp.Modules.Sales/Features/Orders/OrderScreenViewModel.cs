using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

/// <summary>One line of an order as it is shown, with its amounts as text.</summary>
internal sealed record OrderLineRow(OrderLineDto Line, string QuantityText, string PriceText, string AmountText);

/// <summary>An order on screen: what was agreed, the progress of its pick list, and the button to cancel it.</summary>
internal sealed partial class OrderScreenViewModel(
    Guid orderId,
    GetOrder.Handler get,
    CancelOrder.Handler cancel,
    CreateInvoiceFromOrder.Handler createInvoice,
    ILocalizationService localization) : ViewModelBase
{
    private OrderDetailDto? _detail;

    public ObservableCollection<OrderLineRow> Lines { get; } = [];

    public string Heading => _detail?.Order.Number ?? string.Empty;

    public string StatusText => _detail is null ? string.Empty : localization.Get($"OrderStatus_{_detail.Order.Status}");

    public string CustomerText => _detail is null
        ? string.Empty
        : _detail.CustomerAddress is { } address
            ? $"{_detail.Order.CustomerName}\n{address}"
            : _detail.Order.CustomerName;

    public string OfferText => _detail is null ? string.Empty : localization.Format("OrderScreen_FromOffer", _detail.Order.OfferNumber);

    public string? Reference => _detail?.Reference;

    public string? Notes => _detail?.Notes;

    public string PickText => _detail switch
    {
        null => string.Empty,
        { PickListNumber: null } => localization.Get("OrderScreen_NothingToPick"),
        { Order: var order, PickListNumber: var number } => localization.Format("OrderScreen_PickList", number, order.PickedLines, order.PickLines),
    };

    public string NetText => Format(_detail?.Totals.Net);

    public string VatText => Format(_detail?.Totals.Vat);

    public string TotalText => Format(_detail?.Totals.Gross);

    public bool CanCancel => _detail is { Order.Status: not (OrderStatus.Cancelled or OrderStatus.Invoiced) };

    public bool CanInvoice => _detail is { Order.Status: OrderStatus.ReadyToInvoice };

    public string InvoiceText => _detail?.InvoiceNumber is { } number ? localization.Format("OrderScreen_HasInvoice", number) : string.Empty;

    [ObservableProperty]
    public partial bool IsConfirmingCancel { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public event EventHandler<bool>? Closed;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await get.HandleAsync(new GetOrder.Query(orderId), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        Apply(result.Value);
    }

    [RelayCommand]
    private void RequestCancel() => IsConfirmingCancel = true;

    [RelayCommand]
    private void CancelCancel() => IsConfirmingCancel = false;

    [RelayCommand]
    private async Task ConfirmCancelAsync(CancellationToken cancellationToken)
    {
        IsConfirmingCancel = false;
        var result = await cancel.HandleAsync(new CancelOrder.Command(orderId), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = localization.Get("OrderScreen_Cancelled");
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [ObservableProperty]
    public partial decimal? PaymentDays { get; set; } = CreateInvoiceFromOrder.DefaultPaymentDays;

    [RelayCommand]
    private async Task CreateInvoiceAsync(CancellationToken cancellationToken)
    {
        var days = (int)(PaymentDays ?? CreateInvoiceFromOrder.DefaultPaymentDays);
        var result = await createInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(orderId, days), cancellationToken);
        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
            StatusMessage = localization.Format("OrderScreen_InvoiceCreated", result.Value.Invoice.Number);
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, true);

    private static string Format(decimal? amount) => (amount ?? 0).ToString("N2", CultureInfo.CurrentCulture);

    private void Apply(OrderDetailDto detail)
    {
        _detail = detail;

        Lines.Clear();
        foreach (var line in detail.Lines)
        {
            Lines.Add(new OrderLineRow(
                line,
                string.Create(CultureInfo.CurrentCulture, $"{line.Quantity:0.##} {localization.Find($"ProductUnit_{line.Unit}") ?? line.Unit}"),
                line.UnitPrice.ToString("N2", CultureInfo.CurrentCulture),
                line.Net.ToString("N2", CultureInfo.CurrentCulture)));
        }

        foreach (var name in new[]
        {
            nameof(Heading), nameof(StatusText), nameof(CustomerText), nameof(OfferText), nameof(Reference), nameof(Notes),
            nameof(PickText), nameof(NetText), nameof(VatText), nameof(TotalText), nameof(CanCancel), nameof(CanInvoice), nameof(InvoiceText),
        })
        {
            OnPropertyChanged(name);
        }
    }
}

using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>One line of an invoice as it is shown, with its amounts as text.</summary>
internal sealed record InvoiceLineRow(InvoiceLineDto Line, string QuantityText, string PriceText, string VatText, string AmountText);

/// <summary>An invoice on screen: what is invoiced, when it is due, and the buttons to record the payment or cancel it.</summary>
internal sealed partial class InvoiceScreenViewModel(
    Guid invoiceId,
    GetInvoice.Handler get,
    MarkInvoicePaid.Handler markPaid,
    CancelInvoice.Handler cancel,
    ILocalizationService localization) : ViewModelBase
{
    private InvoiceDetailDto? _detail;

    public ObservableCollection<InvoiceLineRow> Lines { get; } = [];

    public string Heading => _detail?.Invoice.Number ?? string.Empty;

    public string StatusText => _detail is null
        ? string.Empty
        : localization.Get(_detail.Invoice.IsOverdue ? "InvoiceStatus_Overdue" : $"InvoiceStatus_{_detail.Invoice.Status}");

    public string CustomerText => _detail is null
        ? string.Empty
        : _detail.CustomerAddress is { } address
            ? $"{_detail.Invoice.CustomerName}\n{address}"
            : _detail.Invoice.CustomerName;

    public string DatesText => _detail is null
        ? string.Empty
        : localization.Format("InvoiceScreen_Dates", Date(_detail.Invoice.Date), Date(_detail.Invoice.DueDate));

    public string PaidText => _detail?.Invoice.PaidDate is { } paid ? localization.Format("InvoiceScreen_PaidOn", Date(paid)) : string.Empty;

    public string OrderText => _detail is null ? string.Empty : localization.Format("InvoiceScreen_FromOrder", _detail.Invoice.OrderNumber);

    public string? Reference => _detail?.Reference;

    public string? Notes => _detail?.Notes;

    public string NetText => Format(_detail?.Totals.Net);

    public string VatText => Format(_detail?.Totals.Vat);

    public string TotalText => Format(_detail?.Totals.Gross);

    public bool IsUnpaid => _detail is { Invoice.Status: InvoiceStatus.Unpaid };

    public bool IsPaid => _detail is { Invoice.Status: InvoiceStatus.Paid };

    [ObservableProperty]
    public partial bool IsConfirmingCancel { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public event EventHandler? Closed;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await get.HandleAsync(new GetInvoice.Query(invoiceId), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task MarkPaidAsync(CancellationToken cancellationToken)
    {
        var result = await markPaid.HandleAsync(new MarkInvoicePaid.Command(invoiceId), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = localization.Get("InvoiceScreen_MarkedPaid");
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void RequestCancel() => IsConfirmingCancel = true;

    [RelayCommand]
    private void CancelCancel() => IsConfirmingCancel = false;

    [RelayCommand]
    private async Task ConfirmCancelAsync(CancellationToken cancellationToken)
    {
        IsConfirmingCancel = false;
        var result = await cancel.HandleAsync(new CancelInvoice.Command(invoiceId), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = localization.Get("InvoiceScreen_Cancelled");
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    private static string Date(DateOnly date) => date.ToString("d", CultureInfo.CurrentCulture);

    private static string Format(decimal? amount) => (amount ?? 0).ToString("N2", CultureInfo.CurrentCulture);

    private void Apply(InvoiceDetailDto detail)
    {
        _detail = detail;

        Lines.Clear();
        foreach (var line in detail.Lines)
        {
            Lines.Add(new InvoiceLineRow(
                line,
                string.Create(CultureInfo.CurrentCulture, $"{line.Quantity:0.##} {localization.Find($"ProductUnit_{line.Unit}") ?? line.Unit}"),
                line.UnitPrice.ToString("N2", CultureInfo.CurrentCulture),
                string.Create(CultureInfo.CurrentCulture, $"{line.VatRate:0.##} %"),
                line.Net.ToString("N2", CultureInfo.CurrentCulture)));
        }

        foreach (var name in new[]
        {
            nameof(Heading), nameof(StatusText), nameof(CustomerText), nameof(DatesText), nameof(PaidText), nameof(OrderText),
            nameof(Reference), nameof(Notes), nameof(NetText), nameof(VatText), nameof(TotalText), nameof(IsUnpaid), nameof(IsPaid),
        })
        {
            OnPropertyChanged(name);
        }
    }
}

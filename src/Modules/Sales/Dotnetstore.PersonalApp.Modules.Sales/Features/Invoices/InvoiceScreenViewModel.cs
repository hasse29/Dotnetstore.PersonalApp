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

/// <summary>One payment on an invoice as it is shown.</summary>
internal sealed record PaymentRow(PaymentDto Payment, string DateText, string KindText, string AmountText, string? ExcessText);

/// <summary>An invoice on screen: what is invoiced, its payments, and the forms to record a payment, use credit or cancel it.</summary>
internal sealed partial class InvoiceScreenViewModel(
    Guid invoiceId,
    GetInvoice.Handler get,
    RecordPayment.Handler recordPayment,
    ApplyCredit.Handler applyCredit,
    DeletePayment.Handler deletePayment,
    CancelInvoice.Handler cancel,
    ILocalizationService localization) : ViewModelBase
{
    private InvoiceDetailDto? _detail;

    public ObservableCollection<InvoiceLineRow> Lines { get; } = [];

    public ObservableCollection<PaymentRow> Payments { get; } = [];

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

    public string OrderText => _detail is null ? string.Empty : localization.Format("InvoiceScreen_FromOrder", _detail.Invoice.OrderNumber);

    public string? Reference => _detail?.Reference;

    public string? Notes => _detail?.Notes;

    public string NetText => Format(_detail?.Totals.Net);

    public string VatText => Format(_detail?.Totals.Vat);

    public string TotalText => Format(_detail?.Totals.Gross);

    public string PaidText => Format(_detail?.Invoice.Paid);

    public string OutstandingText => Format(_detail?.Invoice.Outstanding);

    public string CreditText => _detail is { CustomerCredit: > 0m } detail
        ? localization.Format("InvoiceScreen_Credit", Format(detail.CustomerCredit))
        : string.Empty;

    public bool HasPayments => Payments.Count > 0;

    /// <summary>Payments can be entered, and removed, on any invoice that is not cancelled.</summary>
    public bool CanPay => _detail is { Invoice.Status: not InvoiceStatus.Cancelled };

    public bool CanApplyCredit => _detail is { CustomerCredit: > 0m, Invoice.Outstanding: > 0m } && CanPay;

    public bool CanCancel => _detail is { Invoice.Status: InvoiceStatus.Unpaid };

    [ObservableProperty]
    public partial decimal? PaymentAmount { get; set; }

    /// <summary>Empty means today.</summary>
    [ObservableProperty]
    public partial DateTime? PaymentDate { get; set; }

    [ObservableProperty]
    public partial string? PaymentNote { get; set; }

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
    private async Task RecordPaymentAsync(CancellationToken cancellationToken)
    {
        if (PaymentAmount is not { } amount)
        {
            StatusMessage = localization.Get("Error_payment_amount_invalid");
            return;
        }

        var date = PaymentDate is { } picked ? DateOnly.FromDateTime(picked) : (DateOnly?)null;
        var result = await recordPayment.HandleAsync(new RecordPayment.Command(invoiceId, amount, date, PaymentNote), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        Apply(result.Value.Invoice);
        PaymentNote = null;
        PaymentDate = null;

        // Paying too much is not an error, but it is something to act on: say how much became credit.
        StatusMessage = result.Value.Excess > 0m
            ? localization.Format("InvoiceScreen_Overpaid", Format(result.Value.Excess))
            : localization.Get("InvoiceScreen_PaymentRecorded");
    }

    [RelayCommand]
    private async Task ApplyCreditAsync(CancellationToken cancellationToken)
    {
        if (_detail is null)
        {
            return;
        }

        var amount = Math.Min(_detail.CustomerCredit, _detail.Invoice.Outstanding);
        var result = await applyCredit.HandleAsync(new ApplyCredit.Command(invoiceId, amount), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = localization.Format("InvoiceScreen_CreditApplied", Format(amount));
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(PaymentRow? row, CancellationToken cancellationToken)
    {
        if (row is null)
        {
            return;
        }

        var result = await deletePayment.HandleAsync(new DeletePayment.Command(invoiceId, row.Payment.Id), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = localization.Get("InvoiceScreen_PaymentRemoved");
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

        Payments.Clear();
        foreach (var payment in detail.Payments)
        {
            Payments.Add(new PaymentRow(
                payment,
                Date(payment.Date),
                localization.Get($"PaymentKind_{payment.Kind}") + (string.IsNullOrEmpty(payment.Note) ? string.Empty : $" - {payment.Note}"),
                Format(payment.Amount + payment.Excess),
                payment.Excess > 0m ? localization.Format("InvoiceScreen_PaymentExcess", Format(payment.Excess)) : null));
        }

        // What is left to pay is what the next payment most likely is.
        PaymentAmount = detail.Invoice.Outstanding > 0m ? detail.Invoice.Outstanding : null;

        foreach (var name in new[]
        {
            nameof(Heading), nameof(StatusText), nameof(CustomerText), nameof(DatesText), nameof(OrderText), nameof(Reference), nameof(Notes),
            nameof(NetText), nameof(VatText), nameof(TotalText), nameof(PaidText), nameof(OutstandingText), nameof(CreditText),
            nameof(HasPayments), nameof(CanPay), nameof(CanApplyCredit), nameof(CanCancel),
        })
        {
            OnPropertyChanged(name);
        }
    }
}

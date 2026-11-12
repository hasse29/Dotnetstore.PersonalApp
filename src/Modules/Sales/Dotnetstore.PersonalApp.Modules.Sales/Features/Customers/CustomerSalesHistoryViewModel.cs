using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed record SalesHistoryRow(
    SalesHistoryEntry Entry,
    string KindText,
    string StatusText,
    string DateText,
    string TotalText,
    string FromText);

/// <summary>The "Sales history" tab of a customer: every offer, order and invoice, and what has been invoiced and paid. Read-only.</summary>
internal sealed partial class CustomerSalesHistoryViewModel(
    Guid customerId,
    GetCustomerSalesHistory.Handler history,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SalesHistoryRow> Entries { get; } = [];

    [ObservableProperty]
    public partial string InvoicedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaidText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OutstandingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await history.HandleAsync(new GetCustomerSalesHistory.Query(customerId), cancellationToken);

        Entries.Clear();
        foreach (var entry in response.Entries)
        {
            Entries.Add(new SalesHistoryRow(
                entry,
                localization.Get($"SalesHistory_Kind_{entry.Kind}"),
                localization.Get($"{entry.Kind}Status_{entry.Status}"),
                entry.Date.ToString("d", CultureInfo.CurrentCulture),
                entry.Total.ToString("N2", CultureInfo.CurrentCulture),
                entry.From));
        }

        var summary = response.Summary;
        InvoicedText = summary.Invoiced.ToString("N2", CultureInfo.CurrentCulture);
        PaidText = summary.Paid.ToString("N2", CultureInfo.CurrentCulture);
        OutstandingText = summary.Outstanding.ToString("N2", CultureInfo.CurrentCulture);
        StatusMessage = response.Entries.Count == 0
            ? localization.Get("SalesHistory_Empty")
            : localization.Format("SalesHistory_Count", summary.Offers, summary.Orders, summary.Invoices);
    }
}

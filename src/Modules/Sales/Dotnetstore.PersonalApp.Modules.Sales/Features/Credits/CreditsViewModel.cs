using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;

internal sealed record CreditRow(CustomerCreditDto Credit, string BalanceText);

internal sealed record CreditEntryRow(CreditEntryDto Entry, string DateText, string KindText, string AmountText);

/// <summary>Customers with credit left, from payments that were more than their invoices, and the way to pay it back.</summary>
internal sealed partial class CreditsViewModel(
    ListCredits.Handler list,
    GetCustomerCredit.Handler get,
    RefundCredit.Handler refund,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<CreditRow> Credits { get; } = [];

    public ObservableCollection<CreditEntryRow> Entries { get; } = [];

    public bool HasSelection => SelectedCredit is not null;

    public bool IsEmpty => Credits.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial CreditRow? SelectedCredit { get; set; }

    [ObservableProperty]
    public partial decimal? RefundAmount { get; set; }

    /// <summary>Empty means today.</summary>
    [ObservableProperty]
    public partial DateTime? RefundDate { get; set; }

    [ObservableProperty]
    public partial string? RefundNote { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedCreditChanged(CreditRow? value)
    {
        Entries.Clear();
        RefundAmount = value?.Credit.Balance;
        if (value is not null)
        {
            ShowLedgerCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var selected = SelectedCredit?.Credit.CustomerId;
        var items = await list.HandleAsync(cancellationToken);

        Credits.Clear();
        foreach (var credit in items)
        {
            Credits.Add(new CreditRow(credit, Format(credit.Balance)));
        }

        OnPropertyChanged(nameof(IsEmpty));
        SelectedCredit = Credits.FirstOrDefault(c => c.Credit.CustomerId == selected);
        StatusMessage = localization.Format("Credits_Count", Credits.Count);
    }

    [RelayCommand]
    private async Task ShowLedgerAsync(CancellationToken cancellationToken)
    {
        if (SelectedCredit is not { } selected)
        {
            return;
        }

        var result = await get.HandleAsync(new GetCustomerCredit.Query(selected.Credit.CustomerId), cancellationToken);
        if (result.IsSuccess)
        {
            ApplyLedger(result.Value);
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task RefundAsync(CancellationToken cancellationToken)
    {
        if (SelectedCredit is not { } selected)
        {
            return;
        }

        if (RefundAmount is not { } amount)
        {
            StatusMessage = localization.Get("Error_payment_amount_invalid");
            return;
        }

        var date = RefundDate is { } picked ? DateOnly.FromDateTime(picked) : (DateOnly?)null;
        var result = await refund.HandleAsync(new RefundCredit.Command(selected.Credit.CustomerId, amount, date, RefundNote), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        RefundNote = null;
        RefundDate = null;
        StatusMessage = localization.Format("Credits_Refunded", Format(amount));

        // A used-up credit leaves the list; otherwise the balance and ledger are shown again.
        await LoadAsync(cancellationToken);
        if (SelectedCredit is not null)
        {
            ApplyLedger(result.Value);
        }
    }

    private static string Format(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture);

    private void ApplyLedger(CustomerCreditDetailDto detail)
    {
        Entries.Clear();
        foreach (var entry in detail.Entries)
        {
            Entries.Add(new CreditEntryRow(
                entry,
                entry.Date.ToString("d", CultureInfo.CurrentCulture),
                localization.Get($"CreditEntryKind_{entry.Kind}") + (string.IsNullOrEmpty(entry.Note) ? string.Empty : $" - {entry.Note}"),
                Format(entry.Amount)));
        }
    }
}

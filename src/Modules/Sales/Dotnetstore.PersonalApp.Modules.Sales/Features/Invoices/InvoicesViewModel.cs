using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

/// <summary>An invoice as the list shows it: the DTO plus its texts in the current UI language.</summary>
internal sealed record InvoiceRow(InvoiceDto Invoice, string StatusText, string DateText, string DueDateText, string TotalText);

internal sealed record InvoiceStatusOption(InvoiceStatus? Status, string Label);

/// <summary>The invoices list; opening an invoice replaces the list with the invoice screen.</summary>
internal sealed partial class InvoicesViewModel : ViewModelBase
{
    private readonly ListInvoices.Handler _list;
    private readonly GetInvoice.Handler _get;
    private readonly MarkInvoicePaid.Handler _markPaid;
    private readonly CancelInvoice.Handler _cancel;
    private readonly ILocalizationService _localization;
    private readonly bool _ready;

    public InvoicesViewModel(
        ListInvoices.Handler list,
        GetInvoice.Handler get,
        MarkInvoicePaid.Handler markPaid,
        CancelInvoice.Handler cancel,
        ILocalizationService localization)
    {
        _list = list;
        _get = get;
        _markPaid = markPaid;
        _cancel = cancel;
        _localization = localization;

        StatusOptions =
        [
            new InvoiceStatusOption(null, localization.Get("Invoices_AllStatuses")),
            .. Enum.GetValues<InvoiceStatus>().Select(s => new InvoiceStatusOption(s, localization.Get($"InvoiceStatus_{s}"))),
        ];
        SelectedStatus = StatusOptions[0];
        _ready = true;
    }

    public IReadOnlyList<InvoiceStatusOption> StatusOptions { get; }

    public ObservableCollection<InvoiceRow> Invoices { get; } = [];

    public bool HasScreen => Screen is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreen))]
    public partial InvoiceScreenViewModel? Screen { get; set; }

    [ObservableProperty]
    public partial InvoiceRow? SelectedInvoice { get; set; }

    [ObservableProperty]
    public partial InvoiceStatusOption SelectedStatus { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedInvoiceChanged(InvoiceRow? value)
    {
        if (value is null)
        {
            return;
        }

        var screen = new InvoiceScreenViewModel(value.Invoice.Id, _get, _markPaid, _cancel, _localization);
        screen.Closed += OnScreenClosed;
        Screen = screen;
        screen.LoadCommand.Execute(null);
    }

    partial void OnSelectedStatusChanged(InvoiceStatusOption value)
    {
        if (_ready)
        {
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await _list.HandleAsync(new ListInvoices.Query(Search, SelectedStatus.Status), cancellationToken);

        Invoices.Clear();
        foreach (var invoice in response.Items)
        {
            Invoices.Add(new InvoiceRow(
                invoice,
                _localization.Get(invoice.IsOverdue ? "InvoiceStatus_Overdue" : $"InvoiceStatus_{invoice.Status}"),
                invoice.Date.ToString("d", CultureInfo.CurrentCulture),
                invoice.DueDate.ToString("d", CultureInfo.CurrentCulture),
                invoice.Total.ToString("N2", CultureInfo.CurrentCulture)));
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? _localization.Format("Invoices_ShowingPartial", response.Items.Count, response.TotalCount)
            : _localization.Format("Invoices_Count", response.TotalCount);
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnScreenClosed(object? sender, EventArgs e)
    {
        if (sender is InvoiceScreenViewModel screen)
        {
            screen.Closed -= OnScreenClosed;
        }

        Screen = null;
        SelectedInvoice = null;
        await LoadAsync(CancellationToken.None);
    }
}

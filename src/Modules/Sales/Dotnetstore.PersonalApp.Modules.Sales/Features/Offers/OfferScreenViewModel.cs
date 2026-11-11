using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Products;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal sealed record CustomerOption(Guid Id, string Name);

internal sealed record UnitOption(string Key, string Label);

/// <summary>One editable line of the offer.</summary>
internal sealed partial class OfferLineRow : ObservableObject
{
    public OfferLineRow(IReadOnlyList<UnitOption> units, UnitOption unit, bool isEditable)
    {
        Units = units;
        SelectedUnit = unit;
        IsEditable = isEditable;
    }

    public IReadOnlyList<UnitOption> Units { get; }

    public Guid? ProductId { get; init; }

    public bool IsPhysical { get; init; }

    [ObservableProperty]
    public partial bool IsEditable { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotalText))]
    public partial decimal? Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial UnitOption SelectedUnit { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotalText))]
    public partial decimal? UnitPrice { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotalText))]
    public partial decimal? VatRate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotalText))]
    public partial decimal? DiscountPercent { get; set; } = 0;

    public LineAmounts Amounts => new(Quantity ?? 0, UnitPrice ?? 0, VatRate ?? 0, DiscountPercent ?? 0);

    public string LineTotalText => OfferTotals.LineNet(Amounts).ToString("N2", CultureInfo.CurrentCulture);
}

/// <summary>
/// An offer on screen: its customer and dates, its lines with live totals, and the buttons that move it along. Only a draft can
/// be edited; the buttons of the other statuses only change the status.
/// </summary>
internal sealed partial class OfferScreenViewModel : ViewModelBase
{
    private static readonly string[] UnitKeys = ["Piece", "Hour", "Day", "Month", "License"];

    private readonly CreateOffer.Handler _create;
    private readonly UpdateOffer.Handler _update;
    private readonly GetOffer.Handler _get;
    private readonly ChangeOfferStatus.Handler _changeStatus;
    private readonly DeleteOffer.Handler _delete;
    private readonly CreateOrderFromOffer.Handler _createOrder;
    private readonly ListCustomers.Handler _customers;
    private readonly IProductCatalog _catalog;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _clock;
    private readonly List<UnitOption> _units;
    private Guid? _offerId;

    public OfferScreenViewModel(
        Guid? offerId,
        CreateOffer.Handler create,
        UpdateOffer.Handler update,
        GetOffer.Handler get,
        ChangeOfferStatus.Handler changeStatus,
        DeleteOffer.Handler delete,
        CreateOrderFromOffer.Handler createOrder,
        ListCustomers.Handler customers,
        IProductCatalog catalog,
        ILocalizationService localization,
        TimeProvider clock)
    {
        _createOrder = createOrder;
        _offerId = offerId;
        _create = create;
        _update = update;
        _get = get;
        _changeStatus = changeStatus;
        _delete = delete;
        _customers = customers;
        _catalog = catalog;
        _localization = localization;
        _clock = clock;
        _units = [.. UnitKeys.Select(k => new UnitOption(k, localization.Get($"ProductUnit_{k}")))];

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        Date = today.ToDateTime(TimeOnly.MinValue);
        ValidUntil = today.AddDays(30).ToDateTime(TimeOnly.MinValue);
        Lines.CollectionChanged += (_, e) => OnLinesChanged(e);
    }

    /// <summary>Raised when the user leaves the screen; the argument is true when the list has to be reloaded.</summary>
    public event EventHandler<bool>? Closed;

    public ObservableCollection<CustomerOption> Customers { get; } = [];

    public ObservableCollection<OfferLineRow> Lines { get; } = [];

    public ObservableCollection<ProductSummary> ProductResults { get; } = [];

    public string Heading => Number is { Length: > 0 } number
        ? number
        : _localization.Get("OfferScreen_New");

    public bool Exists => _offerId is not null;

    public bool IsEditable => Status == OfferStatus.Draft;

    public bool ShowSend => Exists && Status == OfferStatus.Draft;

    public bool ShowAnswer => Status == OfferStatus.Sent;

    public bool ShowCreateOrder => Exists && Status == OfferStatus.Accepted && OrderNumber is null;

    public bool HasOrder => OrderNumber is not null;

    public string OrderText => _localization.Format("OfferScreen_HasOrder", OrderNumber ?? string.Empty);

    public bool ShowReopen => Status is OfferStatus.Sent or OfferStatus.Declined;

    public bool ShowDelete => Exists && Status is OfferStatus.Draft or OfferStatus.Declined;

    public string StatusText => _localization.Get($"OfferStatus_{Status}");

    public string NetText => Totals.Net.ToString("N2", CultureInfo.CurrentCulture);

    public string VatText => Totals.Vat.ToString("N2", CultureInfo.CurrentCulture);

    public string TotalText => Totals.Gross.ToString("N2", CultureInfo.CurrentCulture);

    public bool HasLines => Lines.Count > 0;

    public bool HasProductResults => ProductResults.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Heading))]
    public partial string Number { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    [NotifyPropertyChangedFor(nameof(ShowSend))]
    [NotifyPropertyChangedFor(nameof(ShowCreateOrder))]
    [NotifyPropertyChangedFor(nameof(ShowAnswer))]
    [NotifyPropertyChangedFor(nameof(ShowReopen))]
    [NotifyPropertyChangedFor(nameof(ShowDelete))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial OfferStatus Status { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCreateOrder))]
    [NotifyPropertyChangedFor(nameof(HasOrder))]
    [NotifyPropertyChangedFor(nameof(OrderText))]
    public partial string? OrderNumber { get; set; }

    [ObservableProperty]
    public partial CustomerOption? SelectedCustomer { get; set; }

    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    [ObservableProperty]
    public partial DateTime? ValidUntil { get; set; }

    [ObservableProperty]
    public partial string Reference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProductSearch { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ProductSummary? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    private OfferTotal Totals => OfferTotals.Compute(Lines.Select(l => l.Amounts));

    partial void OnStatusChanged(OfferStatus value)
    {
        foreach (var line in Lines)
        {
            line.IsEditable = value == OfferStatus.Draft;
        }
    }

    partial void OnSelectedProductChanged(ProductSummary? value)
    {
        if (value is null)
        {
            return;
        }

        var unit = _units.FirstOrDefault(u => u.Key == value.Unit) ?? AddUnit(value.Unit);
        AddLine(new OfferLineRow(_units, unit, IsEditable)
        {
            ProductId = value.Id,
            IsPhysical = value.IsStockable,
            Description = $"{value.Number} {value.Name}",
            UnitPrice = value.UnitPrice,
            VatRate = value.VatRate,
        });

        ProductResults.Clear();
        OnPropertyChanged(nameof(HasProductResults));
        ProductSearch = string.Empty;
        SelectedProduct = null;
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var customers = await _customers.HandleAsync(new ListCustomers.Query(ActiveOnly: true), cancellationToken);
        Customers.Clear();
        foreach (var customer in customers.Items)
        {
            Customers.Add(new CustomerOption(customer.Id, customer.Name));
        }

        if (_offerId is not { } id)
        {
            return;
        }

        var result = await _get.HandleAsync(new GetOffer.Query(id), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (await SaveCoreAsync(cancellationToken))
        {
            StatusMessage = _localization.Get("OfferScreen_Saved");
        }
    }

    [RelayCommand]
    private Task SendAsync(CancellationToken cancellationToken) => MoveAsync(OfferStatus.Sent, saveFirst: true, cancellationToken);

    [RelayCommand]
    private Task AcceptAsync(CancellationToken cancellationToken) => MoveAsync(OfferStatus.Accepted, saveFirst: false, cancellationToken);

    [RelayCommand]
    private Task DeclineAsync(CancellationToken cancellationToken) => MoveAsync(OfferStatus.Declined, saveFirst: false, cancellationToken);

    [RelayCommand]
    private Task ReopenAsync(CancellationToken cancellationToken) => MoveAsync(OfferStatus.Draft, saveFirst: false, cancellationToken);

    [RelayCommand]
    private async Task CreateOrderAsync(CancellationToken cancellationToken)
    {
        if (_offerId is not { } id)
        {
            return;
        }

        ErrorMessage = null;
        var result = await _createOrder.HandleAsync(new CreateOrderFromOffer.Command(id), cancellationToken);
        if (result.IsSuccess)
        {
            OrderNumber = result.Value.Order.Number;
            StatusMessage = _localization.Format("OfferScreen_OrderCreated", result.Value.Order.Number);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        IsConfirmingDelete = false;
        if (_offerId is not { } id)
        {
            return;
        }

        var result = await _delete.HandleAsync(new DeleteOffer.Command(id), cancellationToken);
        if (result.IsSuccess)
        {
            Closed?.Invoke(this, true);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, _offerId is not null);

    [RelayCommand]
    private void AddFreeLine() => AddLine(new OfferLineRow(_units, _units[0], IsEditable));

    [RelayCommand]
    private void RemoveLine(OfferLineRow? line)
    {
        if (line is not null)
        {
            Lines.Remove(line);
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync(CancellationToken cancellationToken)
    {
        var found = await _catalog.SearchAsync(ProductSearch, 20, cancellationToken);
        ProductResults.Clear();
        foreach (var product in found)
        {
            ProductResults.Add(product);
        }

        OnPropertyChanged(nameof(HasProductResults));
    }

    private async Task MoveAsync(OfferStatus target, bool saveFirst, CancellationToken cancellationToken)
    {
        if (_offerId is not { } id)
        {
            return;
        }

        // What is on screen is saved first, so that what gets sent is what the user sees.
        if (saveFirst && !await SaveCoreAsync(cancellationToken))
        {
            return;
        }

        ErrorMessage = null;
        var result = await _changeStatus.HandleAsync(new ChangeOfferStatus.Command(id, target), cancellationToken);
        if (result.IsSuccess)
        {
            Apply(result.Value);
            StatusMessage = _localization.Format("OfferScreen_StatusChanged", StatusText);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    private async Task<bool> SaveCoreAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        StatusMessage = null;

        if (Date is not { } date || ValidUntil is not { } validUntil)
        {
            ErrorMessage = _localization.Get("Error_offer_date_required");
            return false;
        }

        var fields = new OfferFields(
            SelectedCustomer?.Id ?? Guid.Empty,
            DateOnly.FromDateTime(date),
            DateOnly.FromDateTime(validUntil),
            Reference,
            Notes,
            [.. Lines.Select(l => new OfferLineFields(l.ProductId, l.Description, l.Quantity ?? 0, l.SelectedUnit.Key, l.UnitPrice ?? 0, l.VatRate ?? 0, l.DiscountPercent ?? 0, l.IsPhysical))]);

        var result = _offerId is { } id
            ? await _update.HandleAsync(new UpdateOffer.Command(id, fields), cancellationToken)
            : await _create.HandleAsync(new CreateOffer.Command(fields), cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = _localization.Describe(result.Error);
            return false;
        }

        Apply(result.Value);
        return true;
    }

    private void Apply(OfferDetailDto detail)
    {
        _offerId = detail.Offer.Id;
        OnPropertyChanged(nameof(Exists));
        Number = detail.Offer.Number;
        Status = detail.Offer.Status;
        OrderNumber = detail.OrderNumber;

        // An inactive customer is not in the picker, but an existing offer still shows its own.
        if (Customers.All(c => c.Id != detail.Offer.CustomerId))
        {
            Customers.Add(new CustomerOption(detail.Offer.CustomerId, detail.Offer.CustomerName));
        }

        SelectedCustomer = Customers.First(c => c.Id == detail.Offer.CustomerId);
        Date = detail.Offer.Date.ToDateTime(TimeOnly.MinValue);
        ValidUntil = detail.Offer.ValidUntil.ToDateTime(TimeOnly.MinValue);
        Reference = detail.Reference ?? string.Empty;
        Notes = detail.Notes ?? string.Empty;

        Lines.Clear();
        foreach (var line in detail.Lines)
        {
            var unit = _units.FirstOrDefault(u => u.Key == line.Unit) ?? AddUnit(line.Unit);
            AddLine(new OfferLineRow(_units, unit, IsEditable)
            {
                ProductId = line.ProductId,
                IsPhysical = line.IsPhysical,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                VatRate = line.VatRate,
                DiscountPercent = line.DiscountPercent,
            });
        }
    }

    private UnitOption AddUnit(string key)
    {
        var option = new UnitOption(key, _localization.Find($"ProductUnit_{key}") ?? key);
        _units.Add(option);
        return option;
    }

    private void AddLine(OfferLineRow row) => Lines.Add(row);

    private void OnLinesChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (OfferLineRow row in e.NewItems)
            {
                row.PropertyChanged += OnLineChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (OfferLineRow row in e.OldItems)
            {
                row.PropertyChanged -= OnLineChanged;
            }
        }

        OnPropertyChanged(nameof(HasLines));
        NotifyTotals();
    }

    private void OnLineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OfferLineRow.LineTotalText))
        {
            NotifyTotals();
        }
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(NetText));
        OnPropertyChanged(nameof(VatText));
        OnPropertyChanged(nameof(TotalText));
    }
}

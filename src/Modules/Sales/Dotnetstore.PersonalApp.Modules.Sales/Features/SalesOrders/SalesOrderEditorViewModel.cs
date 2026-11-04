using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal sealed partial class SalesOrderEditorViewModel : ViewModelBase
{
    private readonly SalesOrderDto? _existing;
    private readonly CreateSalesOrder.Handler _create;
    private readonly UpdateSalesOrder.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesOrderEditorViewModel(
        SalesOrderDto? existing,
        CreateSalesOrder.Handler create,
        UpdateSalesOrder.Handler update,
        IReadOnlyList<OptionDto> customerOptions,
        IReadOnlyList<OptionDto> salesPersonOptions,
        IReadOnlyList<OptionDto> salesTerritoryOptions,
        IReadOnlyList<OptionDto> shipMethodOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        StatusOptions =
        [
            new ChoiceOption("I", localization.Get("SalesOrderChoice_Status_I")),
            new ChoiceOption("A", localization.Get("SalesOrderChoice_Status_A")),
            new ChoiceOption("B", localization.Get("SalesOrderChoice_Status_B")),
            new ChoiceOption("R", localization.Get("SalesOrderChoice_Status_R")),
            new ChoiceOption("S", localization.Get("SalesOrderChoice_Status_S")),
            new ChoiceOption("C", localization.Get("SalesOrderChoice_Status_C")),
        ];
        CustomerOptions = customerOptions;
        SalesPersonOptions = [OptionDto.None(localization.Get("Sales_None")), .. salesPersonOptions];
        TerritoryOptions = [OptionDto.None(localization.Get("Sales_None")), .. salesTerritoryOptions];
        ShipMethodOptions = [OptionDto.None(localization.Get("Sales_None")), .. shipMethodOptions];

        if (existing is not null)
        {
            Number = existing.Number;
            SelectedStatus = StatusOptions.FirstOrDefault(o => o.Code == existing.Status);
            OnlineOrderFlag = existing.OnlineOrderFlag;
            OrderDate = existing.OrderDate.ToDateTime(TimeOnly.MinValue);
            DueDate = existing.DueDate.ToDateTime(TimeOnly.MinValue);
            ShipDate = existing.ShipDate?.ToDateTime(TimeOnly.MinValue);
            PurchaseOrderNumber = existing.PurchaseOrderNumber ?? string.Empty;
            SelectedCustomer = CustomerOptions.FirstOrDefault(o => o.Id == existing.CustomerId);
            SelectedSalesPerson = SalesPersonOptions.FirstOrDefault(o => o.Id == existing.SalesPersonId) ?? SalesPersonOptions[0];
            SelectedTerritory = TerritoryOptions.FirstOrDefault(o => o.Id == existing.TerritoryId) ?? TerritoryOptions[0];
            SelectedShipMethod = ShipMethodOptions.FirstOrDefault(o => o.Id == existing.ShipMethodId) ?? ShipMethodOptions[0];
            TaxAmt = existing.TaxAmt;
            Freight = existing.Freight;
            Comment = existing.Comment ?? string.Empty;
        }
        else
        {
            SelectedStatus = StatusOptions[0];
            OrderDate = clock.GetLocalNow().Date;
            DueDate = clock.GetLocalNow().Date;
            SelectedSalesPerson = SalesPersonOptions[0];
            SelectedTerritory = TerritoryOptions[0];
            SelectedShipMethod = ShipMethodOptions[0];
        }

        if (fixedParentId is { } parentId)
        {
            SelectedCustomer = CustomerOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedCustomer;
            IsParentFixed = true;
        }
    }

    public event EventHandler<SalesOrderDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesOrderEditor_New" : "SalesOrderEditor_Edit");

    public bool IsParentFixed { get; }

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    public IReadOnlyList<ChoiceOption> StatusOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedStatus { get; set; }

    [ObservableProperty]
    public partial bool OnlineOrderFlag { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? OrderDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ShipDate { get; set; }

    [ObservableProperty]
    public partial string PurchaseOrderNumber { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> CustomerOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedCustomer { get; set; }

    public IReadOnlyList<OptionDto> SalesPersonOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesPerson { get; set; }

    public IReadOnlyList<OptionDto> TerritoryOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedTerritory { get; set; }

    public IReadOnlyList<OptionDto> ShipMethodOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedShipMethod { get; set; }

    [ObservableProperty]
    public partial decimal? TaxAmt { get; set; }

    [ObservableProperty]
    public partial decimal? Freight { get; set; }

    [ObservableProperty]
    public partial string Comment { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesOrder.Command(Number, SelectedStatus?.Code ?? string.Empty, OnlineOrderFlag, DateOnly.FromDateTime(OrderDate ?? DateTime.MinValue), DateOnly.FromDateTime(DueDate ?? DateTime.MinValue), ToDate(ShipDate), PurchaseOrderNumber, SelectedCustomer?.Id ?? Guid.Empty, ToId(SelectedSalesPerson), ToId(SelectedTerritory), ToId(SelectedShipMethod), TaxAmt ?? 0m, Freight ?? 0m, Comment), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesOrder.Command(existing.Id, Number, SelectedStatus?.Code ?? string.Empty, OnlineOrderFlag, DateOnly.FromDateTime(OrderDate ?? DateTime.MinValue), DateOnly.FromDateTime(DueDate ?? DateTime.MinValue), ToDate(ShipDate), PurchaseOrderNumber, SelectedCustomer?.Id ?? Guid.Empty, ToId(SelectedSalesPerson), ToId(SelectedTerritory), ToId(SelectedShipMethod), TaxAmt ?? 0m, Freight ?? 0m, Comment), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private static DateOnly? ToDate(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;

    private static Guid? ToId(OptionDto? option) => option is { } o && o.Id != Guid.Empty ? o.Id : null;
}
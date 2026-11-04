using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal sealed partial class PurchaseOrderEditorViewModel : ViewModelBase
{
    private readonly PurchaseOrderDto? _existing;
    private readonly CreatePurchaseOrder.Handler _create;
    private readonly UpdatePurchaseOrder.Handler _update;
    private readonly ILocalizationService _localization;

    public PurchaseOrderEditorViewModel(
        PurchaseOrderDto? existing,
        CreatePurchaseOrder.Handler create,
        UpdatePurchaseOrder.Handler update,
        IReadOnlyList<OptionDto> vendorOptions,
        IReadOnlyList<OptionDto> shipMethodOptions,
        IReadOnlyList<OptionDto> userOptions,
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
            new ChoiceOption("P", localization.Get("PurchaseOrderChoice_Status_P")),
            new ChoiceOption("A", localization.Get("PurchaseOrderChoice_Status_A")),
            new ChoiceOption("R", localization.Get("PurchaseOrderChoice_Status_R")),
            new ChoiceOption("C", localization.Get("PurchaseOrderChoice_Status_C")),
        ];
        BuyerOptions = [OptionDto.None(localization.Get("Purchasing_None")), .. userOptions];
        VendorOptions = vendorOptions;
        ShipMethodOptions = shipMethodOptions;

        if (existing is not null)
        {
            Number = existing.Number;
            SelectedStatus = StatusOptions.FirstOrDefault(o => o.Code == existing.Status);
            SelectedBuyer = BuyerOptions.FirstOrDefault(o => o.Id == existing.BuyerId) ?? BuyerOptions[0];
            SelectedVendor = VendorOptions.FirstOrDefault(o => o.Id == existing.VendorId);
            SelectedShipMethod = ShipMethodOptions.FirstOrDefault(o => o.Id == existing.ShipMethodId);
            OrderDate = existing.OrderDate.ToDateTime(TimeOnly.MinValue);
            ShipDate = existing.ShipDate?.ToDateTime(TimeOnly.MinValue);
            TaxAmt = existing.TaxAmt;
            Freight = existing.Freight;
        }
        else
        {
            SelectedStatus = StatusOptions[0];
            SelectedBuyer = BuyerOptions[0];
            OrderDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedVendor = VendorOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedVendor;
            IsParentFixed = true;
        }
    }

    public event EventHandler<PurchaseOrderDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "PurchaseOrderEditor_New" : "PurchaseOrderEditor_Edit");

    public bool IsParentFixed { get; }

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    public IReadOnlyList<ChoiceOption> StatusOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedStatus { get; set; }

    public IReadOnlyList<OptionDto> BuyerOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedBuyer { get; set; }

    public IReadOnlyList<OptionDto> VendorOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedVendor { get; set; }

    public IReadOnlyList<OptionDto> ShipMethodOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedShipMethod { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? OrderDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ShipDate { get; set; }

    [ObservableProperty]
    public partial decimal? TaxAmt { get; set; }

    [ObservableProperty]
    public partial decimal? Freight { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreatePurchaseOrder.Command(Number, SelectedStatus?.Code ?? string.Empty, ToId(SelectedBuyer), SelectedVendor?.Id ?? Guid.Empty, SelectedShipMethod?.Id ?? Guid.Empty, DateOnly.FromDateTime(OrderDate ?? DateTime.MinValue), ToDate(ShipDate), TaxAmt ?? 0m, Freight ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdatePurchaseOrder.Command(existing.Id, Number, SelectedStatus?.Code ?? string.Empty, ToId(SelectedBuyer), SelectedVendor?.Id ?? Guid.Empty, SelectedShipMethod?.Id ?? Guid.Empty, DateOnly.FromDateTime(OrderDate ?? DateTime.MinValue), ToDate(ShipDate), TaxAmt ?? 0m, Freight ?? 0m), cancellationToken);

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;

internal sealed partial class SalesOrderDetailEditorViewModel : ViewModelBase
{
    private readonly SalesOrderDetailDto? _existing;
    private readonly CreateSalesOrderDetail.Handler _create;
    private readonly UpdateSalesOrderDetail.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesOrderDetailEditorViewModel(
        SalesOrderDetailDto? existing,
        CreateSalesOrderDetail.Handler create,
        UpdateSalesOrderDetail.Handler update,
        IReadOnlyList<OptionDto> salesOrderOptions,
        IReadOnlyList<OptionDto> specialOfferOptions,
        IReadOnlyList<OptionDto> productOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SalesOrderOptions = salesOrderOptions;
        ProductOptions = productOptions;
        SpecialOfferOptions = [OptionDto.None(localization.Get("Sales_None")), .. specialOfferOptions];

        if (existing is not null)
        {
            SelectedSalesOrder = SalesOrderOptions.FirstOrDefault(o => o.Id == existing.SalesOrderId);
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            CarrierTrackingNumber = existing.CarrierTrackingNumber ?? string.Empty;
            OrderQty = existing.OrderQty;
            SelectedSpecialOffer = SpecialOfferOptions.FirstOrDefault(o => o.Id == existing.SpecialOfferId) ?? SpecialOfferOptions[0];
            UnitPrice = existing.UnitPrice;
            UnitPriceDiscount = existing.UnitPriceDiscount;
        }
        else
        {
            SelectedSpecialOffer = SpecialOfferOptions[0];
        }

        if (fixedParentId is { } parentId)
        {
            SelectedSalesOrder = SalesOrderOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedSalesOrder;
            IsParentFixed = true;
        }
    }

    public event EventHandler<SalesOrderDetailDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesOrderDetailEditor_New" : "SalesOrderDetailEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> SalesOrderOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesOrder { get; set; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string CarrierTrackingNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? OrderQty { get; set; }

    public IReadOnlyList<OptionDto> SpecialOfferOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSpecialOffer { get; set; }

    [ObservableProperty]
    public partial decimal? UnitPrice { get; set; }

    [ObservableProperty]
    public partial decimal? UnitPriceDiscount { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesOrderDetail.Command(SelectedSalesOrder?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty, CarrierTrackingNumber, (int)(OrderQty ?? 0), ToId(SelectedSpecialOffer), UnitPrice ?? 0m, UnitPriceDiscount ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesOrderDetail.Command(existing.Id, SelectedSalesOrder?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty, CarrierTrackingNumber, (int)(OrderQty ?? 0), ToId(SelectedSpecialOffer), UnitPrice ?? 0m, UnitPriceDiscount ?? 0m), cancellationToken);

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

    private static Guid? ToId(OptionDto? option) => option is { } o && o.Id != Guid.Empty ? o.Id : null;
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;

internal sealed partial class PurchaseOrderDetailEditorViewModel : ViewModelBase
{
    private readonly PurchaseOrderDetailDto? _existing;
    private readonly CreatePurchaseOrderDetail.Handler _create;
    private readonly UpdatePurchaseOrderDetail.Handler _update;
    private readonly ILocalizationService _localization;

    public PurchaseOrderDetailEditorViewModel(
        PurchaseOrderDetailDto? existing,
        CreatePurchaseOrderDetail.Handler create,
        UpdatePurchaseOrderDetail.Handler update,
        IReadOnlyList<OptionDto> purchaseOrderOptions,
        IReadOnlyList<OptionDto> productOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        PurchaseOrderOptions = purchaseOrderOptions;
        ProductOptions = productOptions;

        if (existing is not null)
        {
            SelectedPurchaseOrder = PurchaseOrderOptions.FirstOrDefault(o => o.Id == existing.PurchaseOrderId);
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            DueDate = existing.DueDate.ToDateTime(TimeOnly.MinValue);
            OrderQty = existing.OrderQty;
            UnitPrice = existing.UnitPrice;
            ReceivedQty = existing.ReceivedQty;
            RejectedQty = existing.RejectedQty;
        }
        else
        {
            DueDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedPurchaseOrder = PurchaseOrderOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedPurchaseOrder;
            IsParentFixed = true;
        }
    }

    public event EventHandler<PurchaseOrderDetailDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "PurchaseOrderDetailEditor_New" : "PurchaseOrderDetailEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> PurchaseOrderOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedPurchaseOrder { get; set; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }

    [ObservableProperty]
    public partial decimal? OrderQty { get; set; }

    [ObservableProperty]
    public partial decimal? UnitPrice { get; set; }

    [ObservableProperty]
    public partial decimal? ReceivedQty { get; set; }

    [ObservableProperty]
    public partial decimal? RejectedQty { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreatePurchaseOrderDetail.Command(SelectedPurchaseOrder?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty, DateOnly.FromDateTime(DueDate ?? DateTime.MinValue), (int)(OrderQty ?? 0), UnitPrice ?? 0m, ReceivedQty ?? 0m, RejectedQty ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdatePurchaseOrderDetail.Command(existing.Id, SelectedPurchaseOrder?.Id ?? Guid.Empty, SelectedProduct?.Id ?? Guid.Empty, DateOnly.FromDateTime(DueDate ?? DateTime.MinValue), (int)(OrderQty ?? 0), UnitPrice ?? 0m, ReceivedQty ?? 0m, RejectedQty ?? 0m), cancellationToken);

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

}
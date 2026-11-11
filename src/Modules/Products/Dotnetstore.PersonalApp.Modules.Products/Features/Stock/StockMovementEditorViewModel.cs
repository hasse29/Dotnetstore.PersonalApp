using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>What the user is registering. Corrections go through an inventory count, so there is no "adjust" here.</summary>
internal enum StockMovementMode
{
    Receipt,
    Issue,
    Transfer,
}

internal sealed record StockMovementModeOption(StockMovementMode Mode, string Label);

/// <param name="Id">Null for the "no supplier" entry.</param>
internal sealed record SupplierOption(Guid? Id, string Name);

/// <summary>Registers a receipt, an issue or a transfer for the product selected in the stock list.</summary>
internal sealed partial class StockMovementEditorViewModel : ViewModelBase
{
    private readonly Guid _productId;
    private readonly ReceiveStock.Handler _receive;
    private readonly IssueStock.Handler _issue;
    private readonly TransferStock.Handler _transfer;
    private readonly ILocalizationService _localization;

    public StockMovementEditorViewModel(
        StockOverviewDto product,
        string unitLabel,
        IReadOnlyList<StockMovementModeOption> modes,
        IReadOnlyList<StorageLocationDto> locations,
        IReadOnlyList<SupplierOption> suppliers,
        ReceiveStock.Handler receive,
        IssueStock.Handler issue,
        TransferStock.Handler transfer,
        ILocalizationService localization)
    {
        _productId = product.ProductId;
        _receive = receive;
        _issue = issue;
        _transfer = transfer;
        _localization = localization;

        Title = $"{product.Number} · {product.Name}";
        UnitLabel = unitLabel;
        Modes = modes;
        Locations = locations;
        Suppliers = suppliers;

        SelectedMode = modes[0];
        SelectedSupplier = suppliers[0];
        SelectedLocation = locations.Count > 0 ? locations[0] : null;
        SelectedToLocation = locations.Count > 1 ? locations[1] : SelectedLocation;
    }

    /// <summary>Raised with the text for the status line after a movement was registered.</summary>
    public event EventHandler<string>? Saved;

    public event EventHandler? Cancelled;

    public string Title { get; }

    public string UnitLabel { get; }

    public IReadOnlyList<StockMovementModeOption> Modes { get; }

    public IReadOnlyList<StorageLocationDto> Locations { get; }

    public IReadOnlyList<SupplierOption> Suppliers { get; }

    public bool IsReceipt => SelectedMode?.Mode == StockMovementMode.Receipt;

    public bool IsTransfer => SelectedMode?.Mode == StockMovementMode.Transfer;

    public string LocationLabel => _localization.Get(IsTransfer ? "StockEditor_FromLocation" : "StockEditor_Location");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReceipt))]
    [NotifyPropertyChangedFor(nameof(IsTransfer))]
    [NotifyPropertyChangedFor(nameof(LocationLabel))]
    public partial StockMovementModeOption? SelectedMode { get; set; }

    [ObservableProperty]
    public partial StorageLocationDto? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial StorageLocationDto? SelectedToLocation { get; set; }

    [ObservableProperty]
    public partial SupplierOption? SelectedSupplier { get; set; }

    [ObservableProperty]
    public partial decimal? Quantity { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        // The pickers and the number box can be empty, which the handlers' value types cannot express.
        if (SelectedLocation is null || (IsTransfer && SelectedToLocation is null))
        {
            ErrorMessage = _localization.Describe(Error.Validation("storage_location_required", "Select a location."));
            return;
        }

        if (Quantity is not { } quantity)
        {
            ErrorMessage = _localization.Describe(Error.Validation("stock_quantity_invalid", "Enter a quantity."));
            return;
        }

        var mode = SelectedMode!.Mode;
        var result = mode switch
        {
            StockMovementMode.Receipt => await _receive.HandleAsync(
                new ReceiveStock.Command(_productId, SelectedLocation.Id, quantity, SelectedSupplier?.Id, Note), cancellationToken),
            StockMovementMode.Issue => await _issue.HandleAsync(
                new IssueStock.Command(_productId, SelectedLocation.Id, quantity, Note), cancellationToken),
            _ => await _transfer.HandleAsync(
                new TransferStock.Command(_productId, SelectedLocation.Id, SelectedToLocation!.Id, quantity, Note), cancellationToken),
        };

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, _localization.Format("Stock_Registered", SelectedMode.Label, Title));
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}

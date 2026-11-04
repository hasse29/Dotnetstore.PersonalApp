using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Transfers;

/// <summary>Moves some of a product from one storage location to another.</summary>
internal sealed partial class StockTransferViewModel(
    TransferStock.Handler transfer,
    ListStorageLocationOptions.Handler listLocations,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<OptionDto> Products { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OptionDto> Locations { get; set; } = [];

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial OptionDto? FromLocation { get; set; }

    [ObservableProperty]
    public partial OptionDto? ToLocation { get; set; }

    [ObservableProperty]
    public partial decimal? Quantity { get; set; }

    [ObservableProperty]
    public partial string Reference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Products = await references.ProductOptionsAsync(cancellationToken);
        Locations = await listLocations.HandleAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task TransferAsync(CancellationToken cancellationToken)
    {
        StatusMessage = null;
        ErrorMessage = null;

        if (SelectedProduct is null || FromLocation is null || ToLocation is null)
        {
            ErrorMessage = localization.Get("StockTransfer_ChooseAll");
            return;
        }

        var command = new TransferStock.Command(
            SelectedProduct.Id,
            FromLocation.Id,
            ToLocation.Id,
            Quantity ?? 0m,
            DateOnly.FromDateTime(clock.GetLocalNow().DateTime),
            Reference);
        var result = await transfer.HandleAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            StatusMessage = localization.Format("StockTransfer_Done", result.Value);
            Quantity = null;
            Reference = string.Empty;
        }
        else
        {
            ErrorMessage = localization.Describe(result.Error);
        }
    }
}

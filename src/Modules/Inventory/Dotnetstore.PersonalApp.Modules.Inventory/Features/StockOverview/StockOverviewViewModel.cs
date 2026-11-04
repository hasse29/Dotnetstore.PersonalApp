using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockOverview;

/// <summary>Where each product is stored and how many there are, with a filter for what is running low.</summary>
internal sealed partial class StockOverviewViewModel(
    ListStockOverview.Handler list,
    ListWarehouseOptions.Handler listWarehouses,
    ILocalizationService localization) : ViewModelBase
{
    private bool _loading;

    public ObservableCollection<StockOverviewRow> Rows { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<OptionDto> Warehouses { get; set; } = [];

    [ObservableProperty]
    public partial OptionDto? SelectedWarehouse { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool OnlyLow { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedWarehouseChanged(OptionDto? value) => ReloadOnFilterChange();

    partial void OnOnlyLowChanged(bool value) => ReloadOnFilterChange();

    private void ReloadOnFilterChange()
    {
        if (!_loading)
        {
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _loading = true;
        try
        {
            if (Warehouses.Count == 0)
            {
                var warehouses = await listWarehouses.HandleAsync(cancellationToken);
                Warehouses = [OptionDto.None(localization.Get("StockOverview_AllWarehouses")), .. warehouses];
                SelectedWarehouse = Warehouses[0];
            }
        }
        finally
        {
            _loading = false;
        }

        var warehouseId = SelectedWarehouse is { } selected && selected.Id != Guid.Empty ? selected.Id : (Guid?)null;
        var response = await list.HandleAsync(new ListStockOverview.Query(Search, warehouseId, OnlyLow), cancellationToken);

        Rows.Clear();
        foreach (var row in response.Rows)
        {
            Rows.Add(row);
        }

        StatusMessage = response.TotalCount > response.Rows.Count
            ? localization.Format("StockOverview_ShowingPartial", response.Rows.Count, response.TotalCount)
            : localization.Format("StockOverview_Count", response.TotalCount);
    }
}

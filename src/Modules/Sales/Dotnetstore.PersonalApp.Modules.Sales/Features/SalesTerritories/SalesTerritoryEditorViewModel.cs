using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;

internal sealed partial class SalesTerritoryEditorViewModel : ViewModelBase
{
    private readonly SalesTerritoryDto? _existing;
    private readonly CreateSalesTerritory.Handler _create;
    private readonly UpdateSalesTerritory.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesTerritoryEditorViewModel(
        SalesTerritoryDto? existing,
        CreateSalesTerritory.Handler create,
        UpdateSalesTerritory.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Name = existing.Name;
            CountryRegionCode = existing.CountryRegionCode;
            RegionGroup = existing.RegionGroup;
            SalesYtd = existing.SalesYtd;
            SalesLastYear = existing.SalesLastYear;
            CostYtd = existing.CostYtd;
            CostLastYear = existing.CostLastYear;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<SalesTerritoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesTerritoryEditor_New" : "SalesTerritoryEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CountryRegionCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RegionGroup { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? SalesYtd { get; set; }

    [ObservableProperty]
    public partial decimal? SalesLastYear { get; set; }

    [ObservableProperty]
    public partial decimal? CostYtd { get; set; }

    [ObservableProperty]
    public partial decimal? CostLastYear { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesTerritory.Command(Name, CountryRegionCode, RegionGroup, SalesYtd ?? 0m, SalesLastYear ?? 0m, CostYtd ?? 0m, CostLastYear ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesTerritory.Command(existing.Id, Name, CountryRegionCode, RegionGroup, SalesYtd ?? 0m, SalesLastYear ?? 0m, CostYtd ?? 0m, CostLastYear ?? 0m), cancellationToken);

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal sealed partial class SalesPersonEditorViewModel : ViewModelBase
{
    private readonly SalesPersonDto? _existing;
    private readonly CreateSalesPerson.Handler _create;
    private readonly UpdateSalesPerson.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesPersonEditorViewModel(
        SalesPersonDto? existing,
        CreateSalesPerson.Handler create,
        UpdateSalesPerson.Handler update,
        IReadOnlyList<OptionDto> salesTerritoryOptions,
        IReadOnlyList<OptionDto> userOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        UserOptions = [OptionDto.None(localization.Get("Sales_None")), .. userOptions];
        TerritoryOptions = [OptionDto.None(localization.Get("Sales_None")), .. salesTerritoryOptions];

        if (existing is not null)
        {
            Name = existing.Name;
            SelectedUser = UserOptions.FirstOrDefault(o => o.Id == existing.UserId) ?? UserOptions[0];
            SelectedTerritory = TerritoryOptions.FirstOrDefault(o => o.Id == existing.TerritoryId) ?? TerritoryOptions[0];
            SalesQuota = existing.SalesQuota;
            Bonus = existing.Bonus;
            CommissionPct = existing.CommissionPct;
            SalesYtd = existing.SalesYtd;
            SalesLastYear = existing.SalesLastYear;
        }
        else
        {
            SelectedUser = UserOptions[0];
            SelectedTerritory = TerritoryOptions[0];
        }
    }

    public event EventHandler<SalesPersonDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesPersonEditor_New" : "SalesPersonEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> UserOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedUser { get; set; }

    public IReadOnlyList<OptionDto> TerritoryOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedTerritory { get; set; }

    [ObservableProperty]
    public partial decimal? SalesQuota { get; set; }

    [ObservableProperty]
    public partial decimal? Bonus { get; set; }

    [ObservableProperty]
    public partial decimal? CommissionPct { get; set; }

    [ObservableProperty]
    public partial decimal? SalesYtd { get; set; }

    [ObservableProperty]
    public partial decimal? SalesLastYear { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesPerson.Command(Name, ToId(SelectedUser), ToId(SelectedTerritory), SalesQuota, Bonus ?? 0m, CommissionPct ?? 0m, SalesYtd ?? 0m, SalesLastYear ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesPerson.Command(existing.Id, Name, ToId(SelectedUser), ToId(SelectedTerritory), SalesQuota, Bonus ?? 0m, CommissionPct ?? 0m, SalesYtd ?? 0m, SalesLastYear ?? 0m), cancellationToken);

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
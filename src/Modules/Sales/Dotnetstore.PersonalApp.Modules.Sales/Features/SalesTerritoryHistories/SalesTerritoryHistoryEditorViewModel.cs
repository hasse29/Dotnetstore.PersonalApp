using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;

internal sealed partial class SalesTerritoryHistoryEditorViewModel : ViewModelBase
{
    private readonly SalesTerritoryHistoryDto? _existing;
    private readonly CreateSalesTerritoryHistory.Handler _create;
    private readonly UpdateSalesTerritoryHistory.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesTerritoryHistoryEditorViewModel(
        SalesTerritoryHistoryDto? existing,
        CreateSalesTerritoryHistory.Handler create,
        UpdateSalesTerritoryHistory.Handler update,
        IReadOnlyList<OptionDto> salesPersonOptions,
        IReadOnlyList<OptionDto> salesTerritoryOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SalesPersonOptions = salesPersonOptions;
        TerritoryOptions = salesTerritoryOptions;

        if (existing is not null)
        {
            SelectedSalesPerson = SalesPersonOptions.FirstOrDefault(o => o.Id == existing.SalesPersonId);
            SelectedTerritory = TerritoryOptions.FirstOrDefault(o => o.Id == existing.TerritoryId);
            StartDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = existing.EndDate?.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            StartDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedSalesPerson = SalesPersonOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedSalesPerson;
            IsParentFixed = true;
        }
    }

    public event EventHandler<SalesTerritoryHistoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesTerritoryHistoryEditor_New" : "SalesTerritoryHistoryEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> SalesPersonOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesPerson { get; set; }

    public IReadOnlyList<OptionDto> TerritoryOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedTerritory { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesTerritoryHistory.Command(SelectedSalesPerson?.Id ?? Guid.Empty, SelectedTerritory?.Id ?? Guid.Empty, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate)), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesTerritoryHistory.Command(existing.Id, SelectedSalesPerson?.Id ?? Guid.Empty, SelectedTerritory?.Id ?? Guid.Empty, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate)), cancellationToken);

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
}
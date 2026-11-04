using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;

internal sealed partial class SalesPersonQuotaHistoryEditorViewModel : ViewModelBase
{
    private readonly SalesPersonQuotaHistoryDto? _existing;
    private readonly CreateSalesPersonQuotaHistory.Handler _create;
    private readonly UpdateSalesPersonQuotaHistory.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesPersonQuotaHistoryEditorViewModel(
        SalesPersonQuotaHistoryDto? existing,
        CreateSalesPersonQuotaHistory.Handler create,
        UpdateSalesPersonQuotaHistory.Handler update,
        IReadOnlyList<OptionDto> salesPersonOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SalesPersonOptions = salesPersonOptions;

        if (existing is not null)
        {
            SelectedSalesPerson = SalesPersonOptions.FirstOrDefault(o => o.Id == existing.SalesPersonId);
            QuotaDate = existing.QuotaDate.ToDateTime(TimeOnly.MinValue);
            SalesQuota = existing.SalesQuota;
        }
        else
        {
            QuotaDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedSalesPerson = SalesPersonOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedSalesPerson;
            IsParentFixed = true;
        }
    }

    public event EventHandler<SalesPersonQuotaHistoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesPersonQuotaHistoryEditor_New" : "SalesPersonQuotaHistoryEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> SalesPersonOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesPerson { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? QuotaDate { get; set; }

    [ObservableProperty]
    public partial decimal? SalesQuota { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesPersonQuotaHistory.Command(SelectedSalesPerson?.Id ?? Guid.Empty, DateOnly.FromDateTime(QuotaDate ?? DateTime.MinValue), SalesQuota ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesPersonQuotaHistory.Command(existing.Id, SelectedSalesPerson?.Id ?? Guid.Empty, DateOnly.FromDateTime(QuotaDate ?? DateTime.MinValue), SalesQuota ?? 0m), cancellationToken);

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
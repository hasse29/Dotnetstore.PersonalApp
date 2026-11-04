using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;

internal sealed partial class CurrencyRateEditorViewModel : ViewModelBase
{
    private readonly CurrencyRateDto? _existing;
    private readonly CreateCurrencyRate.Handler _create;
    private readonly UpdateCurrencyRate.Handler _update;
    private readonly ILocalizationService _localization;

    public CurrencyRateEditorViewModel(
        CurrencyRateDto? existing,
        CreateCurrencyRate.Handler create,
        UpdateCurrencyRate.Handler update,
        IReadOnlyList<OptionDto> currencyOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        FromCurrencyOptions = currencyOptions;
        ToCurrencyOptions = currencyOptions;

        if (existing is not null)
        {
            RateDate = existing.RateDate.ToDateTime(TimeOnly.MinValue);
            SelectedFromCurrency = FromCurrencyOptions.FirstOrDefault(o => o.Id == existing.FromCurrencyId);
            SelectedToCurrency = ToCurrencyOptions.FirstOrDefault(o => o.Id == existing.ToCurrencyId);
            AverageRate = existing.AverageRate;
            EndOfDayRate = existing.EndOfDayRate;
        }
        else
        {
            RateDate = clock.GetLocalNow().Date;
        }
    }

    public event EventHandler<CurrencyRateDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "CurrencyRateEditor_New" : "CurrencyRateEditor_Edit");

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? RateDate { get; set; }

    public IReadOnlyList<OptionDto> FromCurrencyOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedFromCurrency { get; set; }

    public IReadOnlyList<OptionDto> ToCurrencyOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedToCurrency { get; set; }

    [ObservableProperty]
    public partial decimal? AverageRate { get; set; }

    [ObservableProperty]
    public partial decimal? EndOfDayRate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateCurrencyRate.Command(DateOnly.FromDateTime(RateDate ?? DateTime.MinValue), SelectedFromCurrency?.Id ?? Guid.Empty, SelectedToCurrency?.Id ?? Guid.Empty, AverageRate ?? 0m, EndOfDayRate ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateCurrencyRate.Command(existing.Id, DateOnly.FromDateTime(RateDate ?? DateTime.MinValue), SelectedFromCurrency?.Id ?? Guid.Empty, SelectedToCurrency?.Id ?? Guid.Empty, AverageRate ?? 0m, EndOfDayRate ?? 0m), cancellationToken);

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
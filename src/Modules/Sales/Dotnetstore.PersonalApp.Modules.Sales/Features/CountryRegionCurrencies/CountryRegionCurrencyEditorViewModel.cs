using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;

internal sealed partial class CountryRegionCurrencyEditorViewModel : ViewModelBase
{
    private readonly CountryRegionCurrencyDto? _existing;
    private readonly CreateCountryRegionCurrency.Handler _create;
    private readonly UpdateCountryRegionCurrency.Handler _update;
    private readonly ILocalizationService _localization;

    public CountryRegionCurrencyEditorViewModel(
        CountryRegionCurrencyDto? existing,
        CreateCountryRegionCurrency.Handler create,
        UpdateCountryRegionCurrency.Handler update,
        IReadOnlyList<OptionDto> currencyOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        CurrencyOptions = currencyOptions;

        if (existing is not null)
        {
            CountryRegion = existing.CountryRegion;
            SelectedCurrency = CurrencyOptions.FirstOrDefault(o => o.Id == existing.CurrencyId);
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<CountryRegionCurrencyDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "CountryRegionCurrencyEditor_New" : "CountryRegionCurrencyEditor_Edit");

    [ObservableProperty]
    public partial string CountryRegion { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> CurrencyOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedCurrency { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateCountryRegionCurrency.Command(CountryRegion, SelectedCurrency?.Id ?? Guid.Empty), cancellationToken)
            : await _update.HandleAsync(new UpdateCountryRegionCurrency.Command(existing.Id, CountryRegion, SelectedCurrency?.Id ?? Guid.Empty), cancellationToken);

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
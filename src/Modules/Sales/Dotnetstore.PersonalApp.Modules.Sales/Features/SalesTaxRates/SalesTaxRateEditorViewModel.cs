using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;

internal sealed partial class SalesTaxRateEditorViewModel : ViewModelBase
{
    private readonly SalesTaxRateDto? _existing;
    private readonly CreateSalesTaxRate.Handler _create;
    private readonly UpdateSalesTaxRate.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesTaxRateEditorViewModel(
        SalesTaxRateDto? existing,
        CreateSalesTaxRate.Handler create,
        UpdateSalesTaxRate.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        TaxTypeOptions =
        [
            new ChoiceOption("R", localization.Get("SalesTaxRateChoice_TaxType_R")),
            new ChoiceOption("W", localization.Get("SalesTaxRateChoice_TaxType_W")),
            new ChoiceOption("A", localization.Get("SalesTaxRateChoice_TaxType_A")),
        ];

        if (existing is not null)
        {
            Name = existing.Name;
            StateProvince = existing.StateProvince;
            SelectedTaxType = TaxTypeOptions.FirstOrDefault(o => o.Code == existing.TaxType);
            TaxRate = existing.TaxRate;
        }
        else
        {
            SelectedTaxType = TaxTypeOptions[0];
        }
    }

    public event EventHandler<SalesTaxRateDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesTaxRateEditor_New" : "SalesTaxRateEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StateProvince { get; set; } = string.Empty;

    public IReadOnlyList<ChoiceOption> TaxTypeOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedTaxType { get; set; }

    [ObservableProperty]
    public partial decimal? TaxRate { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesTaxRate.Command(Name, StateProvince, SelectedTaxType?.Code ?? string.Empty, TaxRate ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesTaxRate.Command(existing.Id, Name, StateProvince, SelectedTaxType?.Code ?? string.Empty, TaxRate ?? 0m), cancellationToken);

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
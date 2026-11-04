using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;

internal sealed partial class CurrencyEditorViewModel : ViewModelBase
{
    private readonly CurrencyDto? _existing;
    private readonly CreateCurrency.Handler _create;
    private readonly UpdateCurrency.Handler _update;
    private readonly ILocalizationService _localization;

    public CurrencyEditorViewModel(
        CurrencyDto? existing,
        CreateCurrency.Handler create,
        UpdateCurrency.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;



        if (existing is not null)
        {
            Code = existing.Code;
            Name = existing.Name;
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<CurrencyDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "CurrencyEditor_New" : "CurrencyEditor_Edit");

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateCurrency.Command(Code, Name), cancellationToken)
            : await _update.HandleAsync(new UpdateCurrency.Command(existing.Id, Code, Name), cancellationToken);

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
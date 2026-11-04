using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;

internal sealed partial class StoreEditorViewModel : ViewModelBase
{
    private readonly StoreDto? _existing;
    private readonly CreateStore.Handler _create;
    private readonly UpdateStore.Handler _update;
    private readonly ILocalizationService _localization;

    public StoreEditorViewModel(
        StoreDto? existing,
        CreateStore.Handler create,
        UpdateStore.Handler update,
        IReadOnlyList<OptionDto> salesPersonOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SalesPersonOptions = [OptionDto.None(localization.Get("Sales_None")), .. salesPersonOptions];

        if (existing is not null)
        {
            Name = existing.Name;
            SelectedSalesPerson = SalesPersonOptions.FirstOrDefault(o => o.Id == existing.SalesPersonId) ?? SalesPersonOptions[0];
        }
        else
        {
            SelectedSalesPerson = SalesPersonOptions[0];
        }
    }

    public event EventHandler<StoreDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "StoreEditor_New" : "StoreEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> SalesPersonOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesPerson { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateStore.Command(Name, ToId(SelectedSalesPerson)), cancellationToken)
            : await _update.HandleAsync(new UpdateStore.Command(existing.Id, Name, ToId(SelectedSalesPerson)), cancellationToken);

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomerEditorViewModel : ViewModelBase
{
    private readonly CustomerDto? _existing;
    private readonly CreateCustomer.Handler _create;
    private readonly UpdateCustomer.Handler _update;
    private readonly ILocalizationService _localization;

    public CustomerEditorViewModel(
        CustomerDto? existing,
        CreateCustomer.Handler create,
        UpdateCustomer.Handler update,
        IReadOnlyList<OptionDto> storeOptions,
        IReadOnlyList<OptionDto> salesTerritoryOptions,
        IReadOnlyList<OptionDto> clientOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ClientOptions = [OptionDto.None(localization.Get("Sales_None")), .. clientOptions];
        StoreOptions = [OptionDto.None(localization.Get("Sales_None")), .. storeOptions];
        TerritoryOptions = [OptionDto.None(localization.Get("Sales_None")), .. salesTerritoryOptions];

        if (existing is not null)
        {
            Number = existing.Number;
            Name = existing.Name;
            SelectedClient = ClientOptions.FirstOrDefault(o => o.Id == existing.ClientId) ?? ClientOptions[0];
            SelectedStore = StoreOptions.FirstOrDefault(o => o.Id == existing.StoreId) ?? StoreOptions[0];
            SelectedTerritory = TerritoryOptions.FirstOrDefault(o => o.Id == existing.TerritoryId) ?? TerritoryOptions[0];
        }
        else
        {
            SelectedClient = ClientOptions[0];
            SelectedStore = StoreOptions[0];
            SelectedTerritory = TerritoryOptions[0];
        }
    }

    public event EventHandler<CustomerDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "CustomerEditor_New" : "CustomerEditor_Edit");

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> ClientOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedClient { get; set; }

    public IReadOnlyList<OptionDto> StoreOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedStore { get; set; }

    public IReadOnlyList<OptionDto> TerritoryOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedTerritory { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateCustomer.Command(Number, Name, ToId(SelectedClient), ToId(SelectedStore), ToId(SelectedTerritory)), cancellationToken)
            : await _update.HandleAsync(new UpdateCustomer.Command(existing.Id, Number, Name, ToId(SelectedClient), ToId(SelectedStore), ToId(SelectedTerritory)), cancellationToken);

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
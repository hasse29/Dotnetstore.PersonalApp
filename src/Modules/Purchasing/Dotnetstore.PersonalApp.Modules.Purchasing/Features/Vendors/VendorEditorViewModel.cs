using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

internal sealed partial class VendorEditorViewModel : ViewModelBase
{
    private readonly VendorDto? _existing;
    private readonly CreateVendor.Handler _create;
    private readonly UpdateVendor.Handler _update;
    private readonly ILocalizationService _localization;

    public VendorEditorViewModel(
        VendorDto? existing,
        CreateVendor.Handler create,
        UpdateVendor.Handler update,
        IReadOnlyList<OptionDto> clientOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ClientOptions = [OptionDto.None(localization.Get("Purchasing_None")), .. clientOptions];

        if (existing is not null)
        {
            AccountNumber = existing.AccountNumber;
            Name = existing.Name;
            CreditRating = existing.CreditRating;
            PreferredVendorStatus = existing.PreferredVendorStatus;
            ActiveFlag = existing.ActiveFlag;
            PurchasingWebServiceUrl = existing.PurchasingWebServiceUrl ?? string.Empty;
            SelectedClient = ClientOptions.FirstOrDefault(o => o.Id == existing.ClientId) ?? ClientOptions[0];
        }
        else
        {
            SelectedClient = ClientOptions[0];
        }
    }

    public event EventHandler<VendorDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "VendorEditor_New" : "VendorEditor_Edit");

    [ObservableProperty]
    public partial string AccountNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? CreditRating { get; set; }

    [ObservableProperty]
    public partial bool PreferredVendorStatus { get; set; }

    [ObservableProperty]
    public partial bool ActiveFlag { get; set; }

    [ObservableProperty]
    public partial string PurchasingWebServiceUrl { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> ClientOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedClient { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateVendor.Command(AccountNumber, Name, (int)(CreditRating ?? 0), PreferredVendorStatus, ActiveFlag, PurchasingWebServiceUrl, ToId(SelectedClient)), cancellationToken)
            : await _update.HandleAsync(new UpdateVendor.Command(existing.Id, AccountNumber, Name, (int)(CreditRating ?? 0), PreferredVendorStatus, ActiveFlag, PurchasingWebServiceUrl, ToId(SelectedClient)), cancellationToken);

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;

internal sealed partial class AddressEditorViewModel : ViewModelBase
{
    private readonly AddressDto? _existing;
    private readonly Guid _objectId;
    private readonly CreateAddress.Handler _create;
    private readonly UpdateAddress.Handler _update;
    private readonly ILocalizationService _localization;

    public AddressEditorViewModel(
        AddressDto? existing,
        Guid objectId,
        IReadOnlyList<AddressTypeDto> types,
        CreateAddress.Handler create,
        UpdateAddress.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _objectId = objectId;
        _create = create;
        _update = update;
        _localization = localization;
        Types = types;

        SelectedType = types.FirstOrDefault(t => t.Id == existing?.AddressTypeId) ?? (types.Count > 0 ? types[0] : null);

        if (existing is not null)
        {
            CareOf = existing.CareOf ?? string.Empty;
            Street1 = existing.Street1;
            Street2 = existing.Street2 ?? string.Empty;
            PostalCode = existing.PostalCode;
            City = existing.City;
            State = existing.State ?? string.Empty;
            Country = existing.Country;
        }
    }

    public event EventHandler<AddressDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<AddressTypeDto> Types { get; }

    public string Title => _localization.Get(_existing is null ? "AddressEditor_New" : "AddressEditor_Edit");

    [ObservableProperty]
    public partial AddressTypeDto? SelectedType { get; set; }

    [ObservableProperty]
    public partial string CareOf { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Street1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Street2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PostalCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string State { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Country { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var typeId = SelectedType?.Id ?? Guid.Empty;
        var result = _existing is null
            ? await _create.HandleAsync(
                new CreateAddress.Command(_objectId, typeId, CareOf, Street1, Street2, PostalCode, City, State, Country), cancellationToken)
            : await _update.HandleAsync(
                new UpdateAddress.Command(_existing.Id, typeId, CareOf, Street1, Street2, PostalCode, City, State, Country), cancellationToken);

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

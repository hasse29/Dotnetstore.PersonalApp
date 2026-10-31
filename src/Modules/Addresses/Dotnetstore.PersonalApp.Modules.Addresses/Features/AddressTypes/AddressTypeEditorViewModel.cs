using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

internal sealed partial class AddressTypeEditorViewModel : ViewModelBase
{
    private readonly AddressTypeDto? _existing;
    private readonly CreateAddressType.Handler _create;
    private readonly UpdateAddressType.Handler _update;
    private readonly ILocalizationService _localization;

    public AddressTypeEditorViewModel(
        AddressTypeDto? existing,
        CreateAddressType.Handler create,
        UpdateAddressType.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            Name = existing.Name;
            Description = existing.Description ?? string.Empty;
        }
    }

    public event EventHandler<AddressTypeDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "AddressTypeEditor_New" : "AddressTypeEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is null
            ? await _create.HandleAsync(new CreateAddressType.Command(Name, Description), cancellationToken)
            : await _update.HandleAsync(new UpdateAddressType.Command(_existing.Id, Name, Description), cancellationToken);

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

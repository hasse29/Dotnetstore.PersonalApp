using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

internal sealed partial class EmailAddressEditorViewModel : ViewModelBase
{
    private readonly EmailAddressDto? _existing;
    private readonly Guid _objectId;
    private readonly CreateEmailAddress.Handler _create;
    private readonly UpdateEmailAddress.Handler _update;
    private readonly ILocalizationService _localization;

    public EmailAddressEditorViewModel(
        EmailAddressDto? existing,
        Guid objectId,
        IReadOnlyList<EmailAddressTypeDto> types,
        CreateEmailAddress.Handler create,
        UpdateEmailAddress.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _objectId = objectId;
        _create = create;
        _update = update;
        _localization = localization;
        Types = types;

        SelectedType = types.FirstOrDefault(t => t.Id == existing?.EmailAddressTypeId) ?? (types.Count > 0 ? types[0] : null);

        if (existing is not null)
        {
            Email = existing.Email;
        }
    }

    public event EventHandler<EmailAddressDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<EmailAddressTypeDto> Types { get; }

    public string Title => _localization.Get(_existing is null ? "EmailAddressEditor_New" : "EmailAddressEditor_Edit");

    [ObservableProperty]
    public partial EmailAddressTypeDto? SelectedType { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var typeId = SelectedType?.Id ?? Guid.Empty;
        var result = _existing is null
            ? await _create.HandleAsync(new CreateEmailAddress.Command(_objectId, typeId, Email), cancellationToken)
            : await _update.HandleAsync(new UpdateEmailAddress.Command(_existing.Id, typeId, Email), cancellationToken);

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
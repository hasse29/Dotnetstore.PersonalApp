using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

internal sealed partial class PhoneNumberEditorViewModel : ViewModelBase
{
    private readonly PhoneNumberDto? _existing;
    private readonly Guid _objectId;
    private readonly CreatePhoneNumber.Handler _create;
    private readonly UpdatePhoneNumber.Handler _update;
    private readonly ILocalizationService _localization;

    public PhoneNumberEditorViewModel(
        PhoneNumberDto? existing,
        Guid objectId,
        IReadOnlyList<PhoneNumberTypeDto> types,
        CreatePhoneNumber.Handler create,
        UpdatePhoneNumber.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _objectId = objectId;
        _create = create;
        _update = update;
        _localization = localization;
        Types = types;

        SelectedType = types.FirstOrDefault(t => t.Id == existing?.PhoneNumberTypeId) ?? (types.Count > 0 ? types[0] : null);

        if (existing is not null)
        {
            Number = existing.Number;
        }
    }

    public event EventHandler<PhoneNumberDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<PhoneNumberTypeDto> Types { get; }

    public string Title => _localization.Get(_existing is null ? "PhoneNumberEditor_New" : "PhoneNumberEditor_Edit");

    [ObservableProperty]
    public partial PhoneNumberTypeDto? SelectedType { get; set; }

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var typeId = SelectedType?.Id ?? Guid.Empty;
        var result = _existing is null
            ? await _create.HandleAsync(new CreatePhoneNumber.Command(_objectId, typeId, Number), cancellationToken)
            : await _update.HandleAsync(new UpdatePhoneNumber.Command(_existing.Id, typeId, Number), cancellationToken);

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
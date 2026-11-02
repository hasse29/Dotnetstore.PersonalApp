using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal sealed partial class ContactPersonEditorViewModel : ViewModelBase
{
    private readonly Guid _clientId;
    private readonly ContactPersonDto? _existing;
    private readonly CreateContactPerson.Handler _create;
    private readonly UpdateContactPerson.Handler _update;
    private readonly ILocalizationService _localization;

    public ContactPersonEditorViewModel(
        Guid clientId,
        ContactPersonDto? existing,
        CreateContactPerson.Handler create,
        UpdateContactPerson.Handler update,
        ILocalizationService localization)
    {
        _clientId = clientId;
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            FirstName = existing.FirstName;
            LastName = existing.LastName;
            JobTitle = existing.JobTitle ?? string.Empty;
            Notes = existing.Notes ?? string.Empty;
        }
    }

    public event EventHandler<ContactPersonDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ContactPersonEditor_New" : "ContactPersonEditor_Edit");

    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JobTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is null
            ? await _create.HandleAsync(new CreateContactPerson.Command(_clientId, FirstName, LastName, JobTitle, Notes), cancellationToken)
            : await _update.HandleAsync(new UpdateContactPerson.Command(_existing.Id, FirstName, LastName, JobTitle, Notes), cancellationToken);

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

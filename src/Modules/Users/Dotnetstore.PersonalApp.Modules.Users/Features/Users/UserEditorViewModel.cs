using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal sealed partial class UserEditorViewModel : ViewModelBase
{
    private readonly UserDto? _existing;
    private readonly CreateUser.Handler _create;
    private readonly UpdateUser.Handler _update;
    private readonly ILocalizationService _localization;

    public UserEditorViewModel(UserDto? existing, CreateUser.Handler create, UpdateUser.Handler update, ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is not null)
        {
            FirstName = existing.FirstName;
            LastName = existing.LastName;
            Username = existing.Username;
            DateOfBirth = existing.DateOfBirth.ToDateTime(TimeOnly.MinValue);
        }
    }

    public event EventHandler<UserDto>? Saved;

    public event EventHandler? Cancelled;

    public bool IsNew => _existing is null;

    public string Title => _localization.Get(IsNew ? "Editor_New" : "Editor_Edit");

    public string PasswordPlaceholder => _localization.Get(IsNew ? "Field_Password" : "Editor_PasswordKeep");

    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? DateOfBirth { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (DateOfBirth is not { } dob)
        {
            ErrorMessage = _localization.Get("Error_date_of_birth_required");
            return;
        }

        var dateOfBirth = DateOnly.FromDateTime(dob);
        var result = _existing is null
            ? await _create.HandleAsync(new CreateUser.Command(FirstName, LastName, Username, Password, dateOfBirth), cancellationToken)
            : await _update.HandleAsync(new UpdateUser.Command(_existing.Id, FirstName, LastName, Username, Password, dateOfBirth), cancellationToken);

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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;

internal sealed partial class FirstUserViewModel(
    CreateUser.Handler create,
    ILocalizationService localization,
    Action onCreated) : ViewModelBase
{
    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? DateOfBirth { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task CreateAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (DateOfBirth is not { } dob)
        {
            ErrorMessage = localization.Get("Error_date_of_birth_required");
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = localization.Get("Error_passwords_mismatch");
            return;
        }

        var result = await create.HandleAsync(
            new CreateUser.Command(FirstName, LastName, Username, Password, DateOnly.FromDateTime(dob)),
            cancellationToken);

        if (result.IsSuccess)
        {
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            onCreated();
        }
        else
        {
            ErrorMessage = localization.Describe(result.Error);
        }
    }
}

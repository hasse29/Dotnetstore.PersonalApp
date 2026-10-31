using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal sealed partial class UserDetailViewModel(UserDto user, IObjectAddressesPanelFactory? addressesFactory = null, IObjectEmailAddressesPanelFactory? emailAddressesFactory = null, IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null) : ViewModelBase
{
    public UserDto User { get; } = user;

    /// <summary>The user's addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(user.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(user.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(user.Id);

    public string Initials => string.Concat(
        User.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    public event EventHandler? Closed;

    public event EventHandler? EditRequested;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Edit() => EditRequested?.Invoke(this, EventArgs.Empty);
}

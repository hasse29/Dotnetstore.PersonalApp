using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal sealed partial class ContactPersonDetailViewModel(
    ContactPersonDto person,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    Func<string, PhotoPanelViewModel?>? photoFactory = null) : ViewModelBase
{
    public ContactPersonDto Person { get; } = person;

    /// <summary>The photo with its Change and Remove buttons; null when the host has no file dialog.</summary>
    public PhotoPanelViewModel? Photo { get; } = photoFactory?.Invoke(person.Initials);

    /// <summary>The person's addresses tab, provided by the Addresses module; null when that module is not installed.</summary>
    public ViewModelBase? Addresses { get; } = addressesFactory?.Create(person.Id);

    /// <summary>The email addresses tab, provided by the EmailAddresses module; null when that module is not installed.</summary>
    public ViewModelBase? EmailAddresses { get; } = emailAddressesFactory?.Create(person.Id);

    /// <summary>The phone numbers tab, provided by the PhoneNumbers module; null when that module is not installed.</summary>
    public ViewModelBase? PhoneNumbers { get; } = phoneNumbersFactory?.Create(person.Id);

    public event EventHandler? Closed;

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);
}

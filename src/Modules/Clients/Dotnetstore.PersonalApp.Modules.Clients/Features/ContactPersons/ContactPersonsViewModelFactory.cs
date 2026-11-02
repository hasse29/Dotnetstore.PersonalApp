using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

/// <summary>Builds the contact persons tab for one client; keeps the many handler dependencies out of the client view models.</summary>
internal sealed class ContactPersonsViewModelFactory(
    ListContactPersons.Handler list,
    CreateContactPerson.Handler create,
    UpdateContactPerson.Handler update,
    DeleteContactPerson.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    ContactPersonPhotoPanelFactory? photoFactory = null)
{
    public ContactPersonsViewModel Create(Guid clientId) =>
        new(clientId, list, create, update, delete, localization, addressesFactory, emailAddressesFactory, phoneNumbersFactory, photoFactory);
}

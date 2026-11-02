using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

/// <summary>Builds the students tab for one client; keeps the many handler dependencies out of the client view models.</summary>
internal sealed class StudentsViewModelFactory(
    ListStudents.Handler list,
    CreateStudent.Handler create,
    UpdateStudent.Handler update,
    DeleteStudent.Handler delete,
    ILocalizationService localization,
    IObjectAddressesPanelFactory? addressesFactory = null,
    IObjectEmailAddressesPanelFactory? emailAddressesFactory = null,
    IObjectPhoneNumbersPanelFactory? phoneNumbersFactory = null,
    StudentPhotoPanelFactory? photoFactory = null,
    IClientLookup? clients = null)
{
    /// <summary>The tab on one school's detail page.</summary>
    public StudentsViewModel Create(Guid clientId) => Build(clientId);

    /// <summary>The module's own page, with the students of every school.</summary>
    public StudentsViewModel CreateForPage() => Build(null);

    private StudentsViewModel Build(Guid? clientId) =>
        new(clientId, list, create, update, delete, localization, addressesFactory, emailAddressesFactory, phoneNumbersFactory, photoFactory, clients);
}

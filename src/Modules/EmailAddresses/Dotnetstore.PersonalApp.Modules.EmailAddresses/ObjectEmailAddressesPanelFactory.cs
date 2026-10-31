using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses;

internal sealed class ObjectEmailAddressesPanelFactory(
    ListEmailAddresses.Handler list,
    ListEmailAddressTypes.Handler listTypes,
    CreateEmailAddress.Handler create,
    UpdateEmailAddress.Handler update,
    DeleteEmailAddress.Handler delete,
    DeleteEmailAddressesForObject.Handler deleteAll,
    ILocalizationService localization) : IObjectEmailAddressesPanelFactory
{
    public ViewModelBase Create(Guid objectId) =>
        new ObjectEmailAddressesViewModel(objectId, list, listTypes, create, update, delete, deleteAll, localization);
}

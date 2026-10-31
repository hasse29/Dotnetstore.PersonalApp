using Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Addresses;

internal sealed class ObjectAddressesPanelFactory(
    ListAddresses.Handler list,
    ListAddressTypes.Handler listTypes,
    CreateAddress.Handler create,
    UpdateAddress.Handler update,
    DeleteAddress.Handler delete,
    DeleteAddressesForObject.Handler deleteAll,
    ILocalizationService localization) : IObjectAddressesPanelFactory
{
    public ViewModelBase Create(Guid objectId) =>
        new ObjectAddressesViewModel(objectId, list, listTypes, create, update, delete, deleteAll, localization);
}

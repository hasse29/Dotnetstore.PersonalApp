using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumberTypes;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers;

internal sealed class ObjectPhoneNumbersPanelFactory(
    ListPhoneNumbers.Handler list,
    ListPhoneNumberTypes.Handler listTypes,
    CreatePhoneNumber.Handler create,
    UpdatePhoneNumber.Handler update,
    DeletePhoneNumber.Handler delete,
    DeletePhoneNumbersForObject.Handler deleteAll,
    ILocalizationService localization) : IObjectPhoneNumbersPanelFactory
{
    public ViewModelBase Create(Guid objectId) =>
        new ObjectPhoneNumbersViewModel(objectId, list, listTypes, create, update, delete, deleteAll, localization);
}

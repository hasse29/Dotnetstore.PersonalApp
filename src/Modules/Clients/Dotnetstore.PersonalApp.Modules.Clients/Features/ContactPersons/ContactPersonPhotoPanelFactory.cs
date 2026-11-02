using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

/// <summary>Builds the photo panel of a contact person's detail page; absent when the host offers no file dialog.</summary>
internal sealed class ContactPersonPhotoPanelFactory(
    GetContactPersonPhoto.Handler get,
    SetContactPersonPhoto.Handler set,
    ILocalizationService localization,
    IFileOpenService? fileOpen = null)
{
    public PhotoPanelViewModel? Create(Guid contactPersonId, string initials) => fileOpen is null
        ? null
        : new PhotoPanelViewModel(
            initials,
            async ct => (await get.HandleAsync(new GetContactPersonPhoto.Query(contactPersonId), ct)) is { IsSuccess: true } result ? result.Value : null,
            (photo, ct) => set.HandleAsync(new SetContactPersonPhoto.Command(contactPersonId, photo), ct),
            fileOpen,
            localization);
}

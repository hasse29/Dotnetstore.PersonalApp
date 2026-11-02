using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

/// <summary>Builds the photo panel of a client's detail page; absent when the host offers no file dialog.</summary>
internal sealed class ClientPhotoPanelFactory(
    GetClientPhoto.Handler get,
    SetClientPhoto.Handler set,
    ILocalizationService localization,
    IFileOpenService? fileOpen = null)
{
    public PhotoPanelViewModel? Create(Guid clientId, string initials) => fileOpen is null
        ? null
        : new PhotoPanelViewModel(
            initials,
            async ct => (await get.HandleAsync(new GetClientPhoto.Query(clientId), ct)) is { IsSuccess: true } result ? result.Value : null,
            (photo, ct) => set.HandleAsync(new SetClientPhoto.Command(clientId, photo), ct),
            fileOpen,
            localization);
}

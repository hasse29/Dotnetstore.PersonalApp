using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

/// <summary>Builds the photo panel of a user's detail page; absent when the host offers no file dialog.</summary>
internal sealed class UserPhotoPanelFactory(
    GetUserPhoto.Handler get,
    SetUserPhoto.Handler set,
    ILocalizationService localization,
    IFileOpenService? fileOpen = null)
{
    public PhotoPanelViewModel? Create(Guid userId, string initials) => fileOpen is null
        ? null
        : new PhotoPanelViewModel(
            initials,
            async ct => (await get.HandleAsync(new GetUserPhoto.Query(userId), ct)) is { IsSuccess: true } result ? result.Value : null,
            (photo, ct) => set.HandleAsync(new SetUserPhoto.Command(userId, photo), ct),
            fileOpen,
            localization);
}

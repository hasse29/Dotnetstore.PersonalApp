using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

/// <summary>Builds the photo panel of a student's detail page; absent when the host offers no file dialog.</summary>
internal sealed class StudentPhotoPanelFactory(
    GetStudentPhoto.Handler get,
    SetStudentPhoto.Handler set,
    ILocalizationService localization,
    IFileOpenService? fileOpen = null)
{
    public PhotoPanelViewModel? Create(Guid studentId, string initials) => fileOpen is null
        ? null
        : new PhotoPanelViewModel(
            initials,
            async ct => (await get.HandleAsync(new GetStudentPhoto.Query(studentId), ct)) is { IsSuccess: true } result ? result.Value : null,
            (photo, ct) => set.HandleAsync(new SetStudentPhoto.Command(studentId, photo), ct),
            fileOpen,
            localization);
}

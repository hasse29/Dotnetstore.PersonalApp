using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>What the detail page needs to load a CV and change its sections, bundled so the list view model can hand it on.</summary>
internal sealed class CvDetailServices(
    GetCvDetail.Handler get,
    SaveWorkExperience.Handler saveWork,
    SaveEducation.Handler saveEducation,
    SaveCourse.Handler saveCourse,
    SaveLanguageSkill.Handler saveLanguage,
    DeleteCvSectionItem.Handler deleteItem,
    SetDriverLicence.Handler setDriverLicence,
    SetCvContacts.Handler setContacts,
    SetCvPhoto.Handler setPhoto,
    ExportCvToWord.Handler export,
    IFileOpenService fileOpen,
    IFileSaveService fileSave,
    ILocalizationService localization)
{
    public GetCvDetail.Handler Get => get;

    public SaveWorkExperience.Handler SaveWork => saveWork;

    public SaveEducation.Handler SaveEducation => saveEducation;

    public SaveCourse.Handler SaveCourse => saveCourse;

    public SaveLanguageSkill.Handler SaveLanguage => saveLanguage;

    public DeleteCvSectionItem.Handler DeleteItem => deleteItem;

    public SetDriverLicence.Handler SetDriverLicence => setDriverLicence;

    public SetCvContacts.Handler SetContacts => setContacts;

    public SetCvPhoto.Handler SetPhoto => setPhoto;

    public ExportCvToWord.Handler Export => export;

    public IFileOpenService FileOpen => fileOpen;

    public IFileSaveService FileSave => fileSave;

    public ILocalizationService Localization => localization;
}

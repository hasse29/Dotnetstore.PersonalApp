using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Subjects;

internal sealed class UserSubjectsPanelFactory(
    ListTeacherSubjects.Handler listTaught,
    ListSubjects.Handler listCatalogue,
    AddTeacherSubject.Handler add,
    RemoveTeacherSubject.Handler remove,
    ILocalizationService localization,
    IModuleAvailability? availability = null) : IUserSubjectsPanelFactory
{
    public ViewModelBase? Create(Guid userId) =>
        availability?.IsEnabled(SchoolGroup.ModuleKey) == false ? null : new UserSubjectsViewModel(userId, listTaught, listCatalogue, add, remove, localization);
}

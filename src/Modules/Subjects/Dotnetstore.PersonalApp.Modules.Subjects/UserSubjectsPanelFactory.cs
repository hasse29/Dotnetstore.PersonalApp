using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Subjects;

internal sealed class UserSubjectsPanelFactory(
    ListTeacherSubjects.Handler listTaught,
    ListSubjects.Handler listCatalogue,
    AddTeacherSubject.Handler add,
    RemoveTeacherSubject.Handler remove,
    ILocalizationService localization) : IUserSubjectsPanelFactory
{
    public ViewModelBase Create(Guid userId) => new UserSubjectsViewModel(userId, listTaught, listCatalogue, add, remove, localization);
}

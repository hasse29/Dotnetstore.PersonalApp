using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Grades;

internal sealed class StudentGradesPanelFactory(GradesViewModelFactory factory, ICurrentUser currentUser) : IStudentGradesPanelFactory
{
    public ViewModelBase Create(Guid studentId) => factory.CreateForStudent(currentUser.User?.Id ?? Guid.Empty, studentId);
}

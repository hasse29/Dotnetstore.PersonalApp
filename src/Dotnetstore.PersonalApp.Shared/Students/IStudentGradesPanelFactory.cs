using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Students;

/// <summary>
/// Lets the Students module show a student's grades as a tab on the student's detail page without referencing the Grades module.
/// The returned view model is resolved to a view by the host's <c>ViewLocator</c>.
/// </summary>
public interface IStudentGradesPanelFactory
{
    ViewModelBase Create(Guid studentId);
}

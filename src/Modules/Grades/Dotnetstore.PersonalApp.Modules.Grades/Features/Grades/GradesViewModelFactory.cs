using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

/// <summary>Builds the grades view models; keeps the many handler dependencies out of the page and the student tab.</summary>
internal sealed class GradesViewModelFactory(
    ListGrades.Handler list,
    CreateGrade.Handler create,
    UpdateGrade.Handler update,
    DeleteGrade.Handler delete,
    ListGradeScales.Handler listScales,
    IStudentLookup students,
    ISubjectLookup subjects,
    ILocalizationService localization,
    TimeProvider clock)
{
    /// <summary>One teacher's grades for one student: the tab on the student's detail page.</summary>
    public GradesViewModel CreateForStudent(Guid userId, Guid studentId) => Build(userId, studentId);

    /// <summary>One teacher's grades for every student: the module's own page.</summary>
    public GradesViewModel CreateForPage(Guid userId) => Build(userId, null);

    private GradesViewModel Build(Guid userId, Guid? studentId) =>
        new(userId, studentId, list, create, update, delete, listScales, students, subjects, localization, clock);
}

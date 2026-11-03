using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>Builds the tests view model; keeps the many handler dependencies out of the page.</summary>
internal sealed class TestsViewModelFactory(
    ListTests.Handler list,
    CreateTest.Handler create,
    UpdateTest.Handler update,
    DeleteTest.Handler delete,
    ListTestResults.Handler listResults,
    SaveTestResult.Handler saveResult,
    DeleteTestResult.Handler deleteResult,
    ListGradeScales.Handler listScales,
    IStudentLookup students,
    ILocalizationService localization,
    TimeProvider clock)
{
    /// <summary>One teacher's tests: the module's page.</summary>
    public TestsViewModel Create(Guid userId) =>
        new(userId, list, create, update, delete, listResults, saveResult, deleteResult, listScales, students, localization, clock);
}

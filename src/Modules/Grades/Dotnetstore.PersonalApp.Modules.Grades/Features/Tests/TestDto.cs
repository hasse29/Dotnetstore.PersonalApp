using Dotnetstore.PersonalApp.Modules.Grades.Domain;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal sealed record TestDto(Guid Id, string Name, DateOnly Date, double MaxScore, int ResultCount, DateTimeOffset CreatedAt)
{
    public static TestDto From(Test test, int resultCount) => new(test.Id, test.Name, test.Date, test.MaxScore, resultCount, test.CreatedAt);
}

/// <param name="ScaleId">The scale the grade belongs to; null while the result has no grade.</param>
internal sealed record TestResultDto(
    Guid Id,
    Guid TestId,
    Guid StudentId,
    double Score,
    Guid? ScaleId,
    Guid? ScaleItemId,
    string? GradeLabel,
    bool? IsPassing)
{
    public static TestResultDto From(TestResult result, GradeScaleItem? item) => new(
        result.Id,
        result.TestId,
        result.StudentId,
        result.Score,
        item?.ScaleId,
        item?.Id,
        item?.Label,
        item?.IsPassing);
}

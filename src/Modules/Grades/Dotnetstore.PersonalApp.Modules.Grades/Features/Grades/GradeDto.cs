using Dotnetstore.PersonalApp.Modules.Grades.Domain;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

internal sealed record GradeDto(
    Guid Id,
    Guid StudentId,
    Guid SubjectId,
    Guid ScaleId,
    string ScaleName,
    Guid ScaleItemId,
    string GradeLabel,
    bool IsPassing,
    DateOnly Date,
    string? Comment,
    DateTimeOffset CreatedAt)
{
    public static GradeDto From(Grade grade, GradeScaleItem item, string scaleName) => new(
        grade.Id,
        grade.StudentId,
        grade.SubjectId,
        item.ScaleId,
        scaleName,
        item.Id,
        item.Label,
        item.IsPassing,
        grade.Date,
        grade.Comment,
        grade.CreatedAt);
}

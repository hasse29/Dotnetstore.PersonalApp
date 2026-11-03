namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

/// <summary>A grade with the names of the student and subject, which live in other modules and are looked up by the view model.</summary>
internal sealed record GradeRow(GradeDto Grade, string StudentName, string SubjectName)
{
    public DateOnly Date => Grade.Date;

    public string GradeLabel => Grade.GradeLabel;

    public string ScaleName => Grade.ScaleName;

    public string? Comment => Grade.Comment;
}

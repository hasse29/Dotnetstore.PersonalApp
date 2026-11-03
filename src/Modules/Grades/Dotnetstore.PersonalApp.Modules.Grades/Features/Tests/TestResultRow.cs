namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>A result with the student's name, which lives in another module and is looked up by the view model.</summary>
internal sealed record TestResultRow(TestResultDto Result, string StudentName, double MaxScore)
{
    public string ScoreDisplay => $"{ScoreText.Format(Result.Score)} / {ScoreText.Format(MaxScore)}";

    public string GradeDisplay => Result.GradeLabel ?? "-";
}

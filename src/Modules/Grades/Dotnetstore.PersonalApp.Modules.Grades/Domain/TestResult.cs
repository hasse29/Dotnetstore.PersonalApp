namespace Dotnetstore.PersonalApp.Modules.Grades.Domain;

/// <summary>One student's result on a test: the score and, once the teacher has set it, the grade. The student lives in another module.</summary>
internal sealed class TestResult
{
    public Guid Id { get; set; }

    public Guid TestId { get; set; }

    public Test Test { get; set; } = null!;

    public Guid StudentId { get; set; }

    public double Score { get; set; }

    /// <summary>The grade on a scale; null while the test has been scored but not graded.</summary>
    public Guid? ScaleItemId { get; set; }

    public GradeScaleItem? ScaleItem { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

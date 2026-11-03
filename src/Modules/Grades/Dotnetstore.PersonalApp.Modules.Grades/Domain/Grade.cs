namespace Dotnetstore.PersonalApp.Modules.Grades.Domain;

/// <summary>A grade a teacher gave a student in a subject. The teacher, student and subject live in other modules, so there are no foreign keys to them.</summary>
internal sealed class Grade
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid StudentId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid ScaleItemId { get; set; }

    public GradeScaleItem ScaleItem { get; set; } = null!;

    /// <summary>The day the grade was given.</summary>
    public DateOnly Date { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

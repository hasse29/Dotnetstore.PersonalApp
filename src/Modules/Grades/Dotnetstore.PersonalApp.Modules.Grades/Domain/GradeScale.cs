namespace Dotnetstore.PersonalApp.Modules.Grades.Domain;

/// <summary>A grading system, e.g. Sweden's A-F or Germany's 1-6; countries differ, so the teacher defines the scales in use.</summary>
internal sealed class GradeScale
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>The country the scale belongs to; free text, only for telling similar scales apart.</summary>
    public string? Country { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<GradeScaleItem> Items { get; set; } = [];
}

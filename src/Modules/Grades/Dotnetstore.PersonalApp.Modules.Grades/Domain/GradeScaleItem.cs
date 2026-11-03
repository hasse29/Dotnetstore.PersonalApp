namespace Dotnetstore.PersonalApp.Modules.Grades.Domain;

/// <summary>One step on a <see cref="GradeScale"/>, e.g. "A" or "4".</summary>
internal sealed class GradeScaleItem
{
    public Guid Id { get; set; }

    public Guid ScaleId { get; set; }

    /// <summary>What is written on the report, e.g. "A", "4" or "Pass".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Orders the steps whatever the labels look like: higher is better.</summary>
    public int Rank { get; set; }

    public string? Description { get; set; }

    public bool IsPassing { get; set; }
}

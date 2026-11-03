namespace Dotnetstore.PersonalApp.Modules.Grades.Domain;

/// <summary>A test a teacher gave. The teacher lives in another module, so there is no foreign key to them.</summary>
internal sealed class Test
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>The day the test was taken.</summary>
    public DateOnly Date { get; set; }

    /// <summary>The highest score a student can get; results are between 0 and this.</summary>
    public double MaxScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<TestResult> Results { get; set; } = [];
}

using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;

internal static class WorkTimeTypeSeed
{
    /// <summary>The type new reports start with, and the one reports from before work time types existed were given.</summary>
    public static readonly Guid WorkId = Guid.Parse("0197c0de-0000-7000-8000-000000000101");

    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly WorkTimeType[] All =
    [
        new() { Id = WorkId, Name = "Work", Description = "Ordinary working time.", CreatedAt = SeededAt },
        Create("0197c0de-0000-7000-8000-000000000102", "Sick", "Absent because of illness."),
        Create("0197c0de-0000-7000-8000-000000000103", "Vacation", "Paid vacation."),
        Create("0197c0de-0000-7000-8000-000000000104", "Parental leave", "Absent to care for a child."),
    ];

    private static WorkTimeType Create(string id, string name, string description) =>
        new() { Id = Guid.Parse(id), Name = name, Description = description, CreatedAt = SeededAt };
}
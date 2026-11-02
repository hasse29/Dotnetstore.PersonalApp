namespace Dotnetstore.PersonalApp.Modules.Schedule.Domain;

/// <summary>
/// One recurring weekly lesson: a teacher (a user) teaches a subject at a school (a client) every <see cref="Day"/> between two times.
/// The teacher, subject and school live in other modules, so there are no foreign keys.
/// </summary>
internal sealed class ScheduleEntry
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid ClientId { get; set; }

    public DayOfWeek Day { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Room { get; set; }

    public string? Notes { get; set; }

    public List<ScheduleEntryStudent> Students { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
}

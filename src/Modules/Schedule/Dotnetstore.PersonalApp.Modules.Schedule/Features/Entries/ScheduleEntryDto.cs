using Dotnetstore.PersonalApp.Modules.Schedule.Domain;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal sealed record ScheduleEntryDto(
    Guid Id,
    DayOfWeek Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SubjectId,
    Guid ClientId,
    string? Room,
    string? Notes,
    IReadOnlyList<Guid> StudentIds)
{
    public static ScheduleEntryDto From(ScheduleEntry entry) =>
        new(entry.Id, entry.Day, entry.StartTime, entry.EndTime, entry.SubjectId, entry.ClientId, entry.Room, entry.Notes, [.. entry.Students.Select(s => s.StudentId)]);
}

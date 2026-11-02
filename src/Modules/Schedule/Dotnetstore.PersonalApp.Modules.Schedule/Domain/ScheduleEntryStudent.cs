namespace Dotnetstore.PersonalApp.Modules.Schedule.Domain;

/// <summary>A student who attends a lesson. <see cref="StudentId"/> is a Students id, so there is no foreign key.</summary>
internal sealed class ScheduleEntryStudent
{
    public Guid EntryId { get; set; }

    public Guid StudentId { get; set; }
}

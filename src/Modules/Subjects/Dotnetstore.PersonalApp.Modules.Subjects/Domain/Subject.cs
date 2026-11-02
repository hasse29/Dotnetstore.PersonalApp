namespace Dotnetstore.PersonalApp.Modules.Subjects.Domain;

/// <summary>A school subject a teacher can teach, e.g. Mathematics. The catalogue is user-managed and shared by every teacher.</summary>
internal sealed class Subject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

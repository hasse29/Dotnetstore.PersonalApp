namespace Dotnetstore.PersonalApp.Modules.Subjects.Domain;

/// <summary>States that a teacher (a user in the Users module, so no foreign key) teaches a subject.</summary>
internal sealed class TeacherSubject
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SubjectId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

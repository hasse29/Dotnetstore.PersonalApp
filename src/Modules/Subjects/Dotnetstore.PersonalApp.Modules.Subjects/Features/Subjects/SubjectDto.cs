using Dotnetstore.PersonalApp.Modules.Subjects.Domain;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal sealed record SubjectDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static SubjectDto From(Subject subject) => new(subject.Id, subject.Name, subject.Description, subject.CreatedAt);
}

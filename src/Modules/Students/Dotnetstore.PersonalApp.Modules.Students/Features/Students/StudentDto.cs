using Dotnetstore.PersonalApp.Modules.Students.Domain;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal sealed record StudentDto(
    Guid Id,
    Guid ClientId,
    string FirstName,
    string LastName,
    string? ClassName,
    string? Notes,
    DateTimeOffset CreatedAt,
    byte[]? Thumbnail = null)
{
    /// <summary>The school's name, filled in by the view model from the Clients module; the database only knows the id.</summary>
    public string? SchoolName { get; init; }

    public string FullName => $"{FirstName} {LastName}";

    public string Initials => string.Concat(char.ToUpperInvariant(FirstName[0]), char.ToUpperInvariant(LastName[0]));

    public static StudentDto From(Student student) => new(
        student.Id,
        student.ClientId,
        student.FirstName,
        student.LastName,
        student.ClassName,
        student.Notes,
        student.CreatedAt,
        student.PhotoThumbnail);
}

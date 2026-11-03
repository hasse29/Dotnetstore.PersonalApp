using Dotnetstore.PersonalApp.Modules.Students.Domain;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

internal sealed record StudentNoteDto(Guid Id, Guid StudentId, DateOnly Date, string Text, DateTimeOffset CreatedAt)
{
    public static StudentNoteDto From(StudentNote note) => new(note.Id, note.StudentId, note.Date, note.Text, note.CreatedAt);
}

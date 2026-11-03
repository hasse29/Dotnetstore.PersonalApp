using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

/// <summary>Builds the notes tab of a student's detail page; keeps the handler dependencies out of the student view models.</summary>
internal sealed class StudentNotesViewModelFactory(
    ListStudentNotes.Handler list,
    AddStudentNote.Handler add,
    UpdateStudentNote.Handler update,
    DeleteStudentNote.Handler delete,
    ILocalizationService localization,
    TimeProvider clock)
{
    public StudentNotesViewModel Create(Guid studentId) => new(studentId, list, add, update, delete, localization, clock);
}

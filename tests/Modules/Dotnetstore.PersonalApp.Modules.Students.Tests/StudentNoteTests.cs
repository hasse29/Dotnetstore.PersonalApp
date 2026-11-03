using Dotnetstore.PersonalApp.Modules.Students.Features.Notes;
using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentNoteTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<StudentDto> CreateStudentAsync(string firstName = "Anna") =>
        (await _fixture.Create.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), firstName, "Svensson", null, null), Ct)).Value;

    private Task<Result<StudentNoteDto>> AddAsync(StudentDto student, int day, string text) =>
        _fixture.AddNote.HandleAsync(new AddStudentNote.Command(student.Id, new DateOnly(2026, 10, day), text), Ct);

    private StudentNotesViewModel BuildViewModel(StudentDto student) =>
        new(student.Id, _fixture.ListNotes, _fixture.AddNote, _fixture.UpdateNote, _fixture.DeleteNote, new Localizer(), _fixture.Clock);

    [Fact]
    public async Task NotesAreListedWithTheLatestDateFirstNoMatterWhenTheyWereWritten()
    {
        var student = await CreateStudentAsync();
        await AddAsync(student, 5, "Middle");
        await AddAsync(student, 20, "Latest");
        await AddAsync(student, 1, "Oldest");

        var notes = await _fixture.ListNotes.HandleAsync(new ListStudentNotes.Query(student.Id), Ct);

        notes.Select(n => n.Text).ShouldBe(["Latest", "Middle", "Oldest"]);
    }

    [Fact]
    public async Task OnlyTheStudentsOwnNotesAreListed()
    {
        var anna = await CreateStudentAsync();
        var olle = await CreateStudentAsync("Olle");
        await AddAsync(anna, 5, "About Anna");
        await AddAsync(olle, 5, "About Olle");

        (await _fixture.ListNotes.HandleAsync(new ListStudentNotes.Query(anna.Id), Ct)).ShouldHaveSingleItem().Text.ShouldBe("About Anna");
    }

    [Fact]
    public async Task ANoteIsTrimmedAndStampedWithTheCreationTime()
    {
        var student = await CreateStudentAsync();

        var note = (await AddAsync(student, 5, "  Struggled with fractions  ")).Value;

        note.Text.ShouldBe("Struggled with fractions");
        note.Date.ShouldBe(new DateOnly(2026, 10, 5));
        note.CreatedAt.ShouldBe(_fixture.Clock.GetUtcNow());
    }

    [Fact]
    public async Task ANoteCanBeChangedAndMovedToAnotherDate()
    {
        var student = await CreateStudentAsync();
        var note = (await AddAsync(student, 5, "First draft")).Value;

        var updated = await _fixture.UpdateNote.HandleAsync(new UpdateStudentNote.Command(note.Id, new DateOnly(2026, 10, 9), "Final text"), Ct);

        updated.Value.Text.ShouldBe("Final text");
        (await _fixture.ListNotes.HandleAsync(new ListStudentNotes.Query(student.Id), Ct)).ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 10, 9));
    }

    [Fact]
    public async Task ADeletedNoteIsGoneAndDeletingItAgainFails()
    {
        var student = await CreateStudentAsync();
        var note = (await AddAsync(student, 5, "Temporary")).Value;

        (await _fixture.DeleteNote.HandleAsync(new DeleteStudentNote.Command(note.Id), Ct)).IsSuccess.ShouldBeTrue();

        (await _fixture.ListNotes.HandleAsync(new ListStudentNotes.Query(student.Id), Ct)).ShouldBeEmpty();
        (await _fixture.DeleteNote.HandleAsync(new DeleteStudentNote.Command(note.Id), Ct)).Error.Code.ShouldBe("student_note_not_found");
    }

    [Fact]
    public async Task AnEmptyOrTooLongNoteIsRejected()
    {
        var student = await CreateStudentAsync();

        (await AddAsync(student, 5, "   ")).Error.Code.ShouldBe("student_note_text_required");
        (await AddAsync(student, 5, new string('x', StudentNoteRules.MaxTextLength + 1))).Error.Code.ShouldBe("student_note_text_too_long");
    }

    [Fact]
    public async Task ANoteForAnUnknownStudentOrAnUnknownNoteIsNotFound()
    {
        var added = await _fixture.AddNote.HandleAsync(new AddStudentNote.Command(Guid.NewGuid(), new DateOnly(2026, 10, 5), "Hello"), Ct);
        var updated = await _fixture.UpdateNote.HandleAsync(new UpdateStudentNote.Command(Guid.NewGuid(), new DateOnly(2026, 10, 5), "Hello"), Ct);

        added.Error.Code.ShouldBe("student_not_found");
        updated.Error.Code.ShouldBe("student_note_not_found");
    }

    [Fact]
    public async Task DeletingAStudentDeletesTheirNotes()
    {
        var student = await CreateStudentAsync();
        await AddAsync(student, 5, "Note");

        await _fixture.Delete.HandleAsync(new DeleteStudent.Command(student.Id), Ct);

        (await _fixture.ListNotes.HandleAsync(new ListStudentNotes.Query(student.Id), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TheViewModelAddsAnEntryClearsTheFormAndShowsItFirst()
    {
        var student = await CreateStudentAsync();
        await AddAsync(student, 1, "Old entry");
        var viewModel = BuildViewModel(student);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Text = "Today's observation";
        await viewModel.SaveCommand.ExecuteAsync(null);

        viewModel.Notes.Select(n => n.Text).ShouldBe(["Today's observation", "Old entry"]);
        viewModel.Text.ShouldBeEmpty();
        viewModel.Notes[0].Date.ShouldBe(new DateOnly(2026, 11, 2));
    }

    [Fact]
    public async Task SelectingAnEntryLoadsItIntoTheFormAndSavingUpdatesIt()
    {
        var student = await CreateStudentAsync();
        await AddAsync(student, 1, "Old entry");
        var viewModel = BuildViewModel(student);
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedNote = viewModel.Notes[0];
        viewModel.IsEditing.ShouldBeTrue();
        viewModel.Text.ShouldBe("Old entry");

        viewModel.Text = "Edited entry";
        await viewModel.SaveCommand.ExecuteAsync(null);

        viewModel.Notes.ShouldHaveSingleItem().Text.ShouldBe("Edited entry");
        viewModel.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public async Task TheViewModelShowsTheErrorForAnEmptyEntry()
    {
        var student = await CreateStudentAsync();
        var viewModel = BuildViewModel(student);

        await viewModel.SaveCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.ShouldBe("Write something in the note.");
    }

    [Fact]
    public async Task TheViewModelDeletesTheSelectedEntryAfterConfirmation()
    {
        var student = await CreateStudentAsync();
        await AddAsync(student, 1, "Old entry");
        var viewModel = BuildViewModel(student);
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedNote = viewModel.Notes[0];

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.Notes.ShouldBeEmpty();
        viewModel.IsConfirmingDelete.ShouldBeFalse();
    }
}

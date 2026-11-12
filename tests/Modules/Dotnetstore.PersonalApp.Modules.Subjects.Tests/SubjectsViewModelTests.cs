using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

public sealed class SubjectsViewModelTests : IDisposable
{
    private readonly SubjectsFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public SubjectsViewModelTests()
    {
        _localizer.AddResources(SubjectsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private SubjectsViewModel List() => new(_fixture.List, _fixture.Create, _fixture.Update, _fixture.Delete, _localizer);

    private SubjectEditorViewModel Editor(SubjectDto? existing = null) => new(existing, _fixture.Create, _fixture.Update, _localizer);

    [Fact]
    public async Task ANewEditorIsEmptyAndTitledAsNew()
    {
        var editor = Editor();

        editor.Title.ShouldBe(_localizer.Get("SubjectEditor_New"));
        editor.Name.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExistingSubjectFillsTheEditor()
    {
        var subject = (await _fixture.Create.HandleAsync(new CreateSubject.Command("Maths", "Numbers"), Ct)).Value;

        var editor = Editor(subject);

        editor.Title.ShouldBe(_localizer.Get("SubjectEditor_Edit"));
        editor.Name.ShouldBe("Maths");
        editor.Description.ShouldBe("Numbers");
    }

    [Fact]
    public async Task SavingANewSubjectRaisesSaved()
    {
        var editor = Editor();
        editor.Name = "Physics";
        SubjectDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Name.ShouldBe("Physics");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingSubjectUpdatesIt()
    {
        var subject = await _fixture.CreateSubjectAsync("Maths", Ct);
        var editor = Editor(subject);
        editor.Name = "Mathematics";
        SubjectDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(subject.Id);
        saved.Name.ShouldBe("Mathematics");
    }

    [Fact]
    public async Task ABlankNameIsRejected()
    {
        var editor = Editor();
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(_localizer.Get("Error_subject_name_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task ADuplicateNameIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await _fixture.CreateSubjectAsync("Maths", Ct);
        var editor = Editor();
        editor.Name = "Maths";

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(_localizer.Describe(Shared.Results.Error.Conflict("subject_conflict", "A subject named 'Maths' already exists.")));

        editor.Name = "Biology";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CancelRaisesCancelled()
    {
        var editor = Editor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task TheListLoadsAndSearches()
    {
        await _fixture.CreateSubjectAsync("Maths", Ct);
        await _fixture.CreateSubjectAsync("Biology", Ct);
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);
        list.Subjects.Count.ShouldBe(2);
        list.StatusMessage.ShouldBe(_localizer.Format("Subjects_Count", 2));

        list.Search = "Bio";
        await list.LoadCommand.ExecuteAsync(null);
        list.Subjects.Single().Name.ShouldBe("Biology");
    }

    [Fact]
    public async Task NewAndSelectOpenTheEditorAndCancelClosesIt()
    {
        await _fixture.CreateSubjectAsync("Maths", Ct);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedSubject = list.Subjects.Single();
        list.Editor.ShouldNotBeNull().Name.ShouldBe("Maths");

        list.NewCommand.Execute(null);
        list.SelectedSubject.ShouldBeNull();
        list.Editor.ShouldNotBeNull().Name.ShouldBeEmpty();

        list.Editor.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItAndReloads()
    {
        var list = List();
        list.NewCommand.Execute(null);
        list.Editor!.Name = "Chemistry";

        await list.Editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("Subjects_Saved", "Chemistry"));
        list.Editor.ShouldBeNull();
        list.Subjects.Single().Name.ShouldBe("Chemistry");
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndCanBeWithdrawn()
    {
        await _fixture.CreateSubjectAsync("Maths", Ct);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        list.SelectedSubject = list.Subjects.Single();

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);

        list.IsConfirmingDelete.ShouldBeFalse();
        list.Subjects.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheSubject()
    {
        await _fixture.CreateSubjectAsync("Maths", Ct);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedSubject = list.Subjects.Single();
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.Subjects.ShouldBeEmpty();
        list.Editor.ShouldBeNull();
        list.IsConfirmingDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingASubjectThatIsAlreadyGoneShowsTheError()
    {
        var subject = await _fixture.CreateSubjectAsync("Maths", Ct);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedSubject = list.Subjects.Single();
        await _fixture.Delete.HandleAsync(new DeleteSubject.Command(subject.Id), Ct);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_localizer.Get("Error_subject_not_found"));
    }
}

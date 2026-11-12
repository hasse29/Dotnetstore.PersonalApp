using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Modules.Cvs.Resources;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;
using System.Windows.Input;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvEditorsViewModelTests : IDisposable
{
    private readonly CvsFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly Guid _userId = Guid.NewGuid();

    public CvEditorsViewModelTests()
    {
        _localizer.AddResources(CvsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private string Error(string code) => _localizer.Get("Error_" + code);

    private CvEditorViewModel Editor(CvDto? existing = null) => new(existing, _userId, _fixture.Create, _fixture.Update, _localizer);

    private async Task<CvDto> AddAsync(string title = "Developer CV") =>
        (await _fixture.Create.HandleAsync(new CreateCv.Command(_userId, title, "Developer", "Summary", string.Empty), Ct)).Value;

    // ---- CV editor ----

    [Fact]
    public async Task ANewCvEditorIsEmptyAndAnExistingOneIsFilled()
    {
        var fresh = Editor();
        fresh.EditorTitle.ShouldBe(_localizer.Get("CvEditor_New"));
        fresh.Title.ShouldBeEmpty();

        var existing = Editor(await AddAsync());
        existing.EditorTitle.ShouldBe(_localizer.Get("CvEditor_Edit"));
        existing.Title.ShouldBe("Developer CV");
        existing.TargetJob.ShouldBe("Developer");
        existing.Summary.ShouldBe("Summary");
    }

    [Fact]
    public async Task SavingANewCvRaisesSaved()
    {
        var editor = Editor();
        editor.Title = "Teacher CV";
        CvDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Title.ShouldBe("Teacher CV");
        saved.UserId.ShouldBe(_userId);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingCvUpdatesIt()
    {
        var cv = await AddAsync();
        var editor = Editor(cv);
        editor.Summary = "A better summary";
        CvDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(cv.Id);
        saved.Summary.ShouldBe("A better summary");
    }

    [Fact]
    public async Task ABlankTitleIsRejected()
    {
        var editor = Editor();
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("cv_title_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task ADuplicateTitleIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await AddAsync();
        var editor = Editor();
        editor.Title = "Developer CV";

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("cv_conflict"));

        editor.Title = "Another CV";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ABadLinkedInAddressIsRejected()
    {
        var editor = Editor();
        editor.Title = "CV";
        editor.LinkedIn = "not a link";

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("cv_linkedin_invalid"));
    }

    [Fact]
    public async Task UpdatingACvThatIsGoneShowsTheError()
    {
        var cv = await AddAsync();
        var editor = Editor(cv with { Id = Guid.NewGuid() });
        editor.Title = "CV";

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("cv_not_found"));
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

    // ---- section editors (course and contacts, through the shared base) ----

    private CourseEditorViewModel CourseEditor(
        CourseDto? existing,
        Func<CourseDraft, CancellationToken, Task<Error?>>? save = null,
        Func<CancellationToken, Task<Error?>>? delete = null) =>
        new(existing, save ?? ((_, _) => Task.FromResult<Error?>(null)), delete, _localizer);

    [Fact]
    public async Task ACourseEditorIsNewWithoutDeleteAndExistingWithIt()
    {
        var fresh = CourseEditor(null);
        fresh.Title.ShouldBe(_localizer.Get("CvCourseEditor_New"));
        fresh.CanDelete.ShouldBeFalse();

        var existing = CourseEditor(new CourseDto(Guid.NewGuid(), "C# course", "Acme Academy", 2024));
        existing.Title.ShouldBe(_localizer.Get("CvCourseEditor_Edit"));
        existing.CanDelete.ShouldBeTrue();
        existing.Name.ShouldBe("C# course");
        existing.Provider.ShouldBe("Acme Academy");
        existing.CompletedYear.ShouldBe(2024);
    }

    [Fact]
    public async Task SavingACourseHandsTheDraftToTheCallerAndRaisesSaved()
    {
        CourseDraft? draft = null;
        var editor = CourseEditor(null, (d, _) =>
        {
            draft = d;
            return Task.FromResult<Error?>(null);
        });
        editor.Name = "C# course";
        editor.Provider = "Acme Academy";
        editor.CompletedYear = 2024;
        var saved = false;
        editor.Saved += (_, _) => saved = true;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldBeTrue();
        draft.ShouldNotBeNull().Name.ShouldBe("C# course");
        draft.CompletedYear.ShouldBe(2024);
        draft.Id.ShouldBeNull();
    }

    [Fact]
    public async Task AnEmptyYearIsSentAsNone()
    {
        CourseDraft? draft = null;
        var editor = CourseEditor(null, (d, _) =>
        {
            draft = d;
            return Task.FromResult<Error?>(null);
        });
        editor.Name = "C# course";

        await editor.SaveCommand.ExecuteAsync(null);

        draft.ShouldNotBeNull().CompletedYear.ShouldBeNull();
    }

    [Fact]
    public async Task ASaveErrorIsShownAndNothingIsRaised()
    {
        var editor = CourseEditor(null, (_, _) => Task.FromResult<Error?>(Shared.Results.Error.Validation("cv_title_required", "Title is required.")));
        var saved = false;
        editor.Saved += (_, _) => saved = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("cv_title_required"));
        saved.ShouldBeFalse();
    }

    [Fact]
    public async Task TheRealCourseHandlerRejectsABlankName()
    {
        var cv = await AddAsync();
        var editor = CourseEditor(
            null,
            async (draft, ct) =>
            {
                var result = await _fixture.SaveCourse.HandleAsync(new SaveCourse.Command(cv.Id, _userId, draft.Id, draft.Name, draft.Provider, draft.CompletedYear), ct);
                return result.IsSuccess ? null : result.Error;
            });

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeletingNeedsConfirmationAndCanBeWithdrawn()
    {
        var editor = CourseEditor(new CourseDto(Guid.NewGuid(), "C# course", null, null), delete: _ => Task.FromResult<Error?>(null));
        var deleted = false;
        editor.Deleted += (_, _) => deleted = true;

        editor.RequestDeleteCommand.Execute(null);
        editor.IsConfirmingDelete.ShouldBeTrue();
        editor.CancelDeleteCommand.Execute(null);
        editor.IsConfirmingDelete.ShouldBeFalse();
        deleted.ShouldBeFalse();

        editor.RequestDeleteCommand.Execute(null);
        await editor.ConfirmDeleteCommand.ExecuteAsync(null);
        deleted.ShouldBeTrue();
    }

    [Fact]
    public async Task AFailedDeleteShowsTheErrorAndClosesTheConfirmation()
    {
        var editor = CourseEditor(
            new CourseDto(Guid.NewGuid(), "C# course", null, null),
            delete: _ => Task.FromResult<Error?>(Shared.Results.Error.NotFound("cv_item_not_found", "Item not found.")));
        var deleted = false;
        editor.Deleted += (_, _) => deleted = true;
        editor.RequestDeleteCommand.Execute(null);

        await editor.ConfirmDeleteCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("cv_item_not_found"));
        editor.IsConfirmingDelete.ShouldBeFalse();
        deleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ASectionEditorCancelRaisesCancelled()
    {
        var editor = CourseEditor(null);
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    // ---- contact editor ----

    private static ContactEntry Entry(ContactKind kind, string type, string value) => new(Guid.NewGuid(), kind, type, value);

    private ContactEditorViewModel Contacts(IReadOnlyList<ContactEntry> available, IReadOnlyList<Guid> selected, Func<IReadOnlyList<Guid>, CancellationToken, Task<Error?>>? save = null) =>
        new(available, selected, save ?? ((_, _) => Task.FromResult<Error?>(null)), _localizer);

    [Fact]
    public async Task TheContactEditorOffersEveryEntryWithItsKindAndTypeAndPreselectsTheChosenOnes()
    {
        var mail = Entry(ContactKind.Email, "Work", "ada@example.com");
        var phone = Entry(ContactKind.Phone, "Mobile", "+46 70 123");

        var editor = Contacts([mail, phone], [phone.Id]);

        editor.Title.ShouldBe(_localizer.Get("CvContactEditor_Title"));
        editor.HasNoOptions.ShouldBeFalse();
        editor.Options.Count.ShouldBe(2);
        var mailOption = editor.Options.Single(o => o.Id == mail.Id);
        mailOption.IsSelected.ShouldBeFalse();
        mailOption.Value.ShouldBe("ada@example.com");
        mailOption.Caption.ShouldBe($"{_localizer.Get("CvContactKind_Email")} · Work");
        editor.Options.Single(o => o.Id == phone.Id).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task WithoutAnyContactEntriesTheEditorSaysSo()
    {
        Contacts([], []).HasNoOptions.ShouldBeTrue();
    }

    [Fact]
    public async Task SavingHandsOverOnlyTheSelectedEntries()
    {
        var mail = Entry(ContactKind.Email, "Work", "ada@example.com");
        var phone = Entry(ContactKind.Phone, "Mobile", "+46 70 123");
        IReadOnlyList<Guid>? chosen = null;
        var editor = Contacts([mail, phone], [], (ids, _) =>
        {
            chosen = ids;
            return Task.FromResult<Error?>(null);
        });
        editor.Options.Single(o => o.Id == mail.Id).IsSelected = true;
        var saved = false;
        editor.Saved += (_, _) => saved = true;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldBeTrue();
        chosen.ShouldBe([mail.Id]);
        editor.CanDelete.ShouldBeFalse();
    }

    // ---- section list ----

    [Fact]
    public async Task ASectionListStartsEmptyAndShowsItsItemsOnceSet()
    {
        var added = 0;
        var list = new CvSectionListViewModel("Courses", "Add course", "No courses yet", () => added++);
        ICommand none = new CommunityToolkit.Mvvm.Input.RelayCommand(() => { });
        list.Title.ShouldBe("Courses");
        list.AddText.ShouldBe("Add course");
        list.EmptyText.ShouldBe("No courses yet");
        list.IsEmpty.ShouldBeTrue();

        list.SetItems([new CvSectionItem("C# course", "Acme", "2024", "Details", none, ["C#", ".NET"])]);
        list.IsEmpty.ShouldBeFalse();
        var item = list.Items.Single();
        item.Title.ShouldBe("C# course");
        item.Subtitle.ShouldBe("Acme");
        item.Meta.ShouldBe("2024");
        item.Description.ShouldBe("Details");
        item.HasTags.ShouldBeTrue();
        item.Tags.ShouldBe(["C#", ".NET"]);

        list.AddCommand.Execute(null);
        added.ShouldBe(1);

        list.SetItems([]);
        list.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task AnItemWithoutTagsHasNone()
    {
        var item = new CvSectionItem("Job", null, null, null, new CommunityToolkit.Mvvm.Input.RelayCommand(() => { }));

        item.HasTags.ShouldBeFalse();
        item.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task ATechnologyChipShowsTheJobCountOnlyWhenThereAreSeveral()
    {
        new TechnologyChip("C#", 1).Text.ShouldBe("C#");
        new TechnologyChip("C#", 3).Text.ShouldBe("C# ×3");
    }
}

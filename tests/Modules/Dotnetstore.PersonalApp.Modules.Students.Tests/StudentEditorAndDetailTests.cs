using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Modules.Students.Resources;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentEditorAndDetailTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly Guid _school = Guid.NewGuid();

    public StudentEditorAndDetailTests()
    {
        _localizer.AddResources(StudentsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private string Error(string code) => _localizer.Get("Error_" + code);

    private StudentEditorViewModel Editor(Guid? clientId, StudentDto? existing = null, IReadOnlyList<ClientReference>? schools = null, Guid? defaultSchool = null) =>
        new(clientId, existing, _fixture.Create, _fixture.Update, _localizer, schools, defaultSchool);

    private async Task<StudentDto> AddAsync() =>
        (await _fixture.Create.HandleAsync(new CreateStudent.Command(_school, "Anna", "Svensson", "7B", "Quiet"), Ct)).Value;

    [Fact]
    public async Task InsideASchoolsTabThereIsNoPickerAndTheTitleSaysNew()
    {
        var editor = Editor(_school);

        editor.ShowSchoolPicker.ShouldBeFalse();
        editor.Title.ShouldBe(_localizer.Get("StudentEditor_New"));
        editor.FirstName.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnExistingStudentFillsTheEditorAndHasNoPicker()
    {
        var editor = Editor(null, await AddAsync());

        editor.ShowSchoolPicker.ShouldBeFalse();
        editor.Title.ShouldBe(_localizer.Get("StudentEditor_Edit"));
        editor.FirstName.ShouldBe("Anna");
        editor.ClassName.ShouldBe("7B");
        editor.Notes.ShouldBe("Quiet");
    }

    [Fact]
    public async Task OutsideASchoolsTabTheUserPicksTheSchoolAndTheDefaultIsPreselected()
    {
        var other = new ClientReference(Guid.NewGuid(), "Other School");
        var schools = new[] { new ClientReference(_school, "Main School"), other };

        var editor = Editor(null, schools: schools, defaultSchool: other.Id);

        editor.ShowSchoolPicker.ShouldBeTrue();
        editor.Schools.Count.ShouldBe(2);
        editor.SelectedSchool.ShouldBe(other);
        Editor(null, schools: schools).SelectedSchool.ShouldBeNull();
    }

    [Fact]
    public async Task SavingInsideASchoolsTabCreatesTheStudentThere()
    {
        var editor = Editor(_school);
        editor.FirstName = "Anna";
        editor.LastName = "Svensson";
        StudentDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().ClientId.ShouldBe(_school);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingFromThePageUsesThePickedSchool()
    {
        var pick = new ClientReference(_school, "Main School");
        var editor = Editor(null, schools: [pick]);
        editor.SelectedSchool = pick;
        editor.FirstName = "Anna";
        editor.LastName = "Svensson";
        StudentDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().ClientId.ShouldBe(_school);
    }

    [Fact]
    public async Task SavingFromThePageWithoutASchoolIsRejected()
    {
        var editor = Editor(null, schools: [new ClientReference(_school, "Main School")]);
        editor.FirstName = "Anna";
        editor.LastName = "Svensson";
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("student_school_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task SavingAnExistingStudentUpdatesIt()
    {
        var student = await AddAsync();
        var editor = Editor(null, student);
        editor.ClassName = "8A";
        StudentDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(student.Id);
        saved.ClassName.ShouldBe("8A");
    }

    [Fact]
    public async Task MissingNamesAreRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        var editor = Editor(_school);
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("student_first_name_required"));

        editor.FirstName = "Anna";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("student_last_name_required"));

        editor.LastName = "Svensson";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CancelRaisesCancelled()
    {
        var editor = Editor(_school);
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task TheDetailShowsTheStudentWithoutOptionalPanelsAndBackRaisesClosed()
    {
        var student = await AddAsync();
        var detail = new StudentDetailViewModel(student);
        var closed = false;
        detail.Closed += (_, _) => closed = true;

        detail.BackCommand.Execute(null);

        detail.Student.ShouldBe(student);
        detail.Photo.ShouldBeNull();
        detail.Addresses.ShouldBeNull();
        detail.EmailAddresses.ShouldBeNull();
        detail.PhoneNumbers.ShouldBeNull();
        detail.Notes.ShouldBeNull();
        detail.Grades.ShouldBeNull();
        closed.ShouldBeTrue();
    }
}

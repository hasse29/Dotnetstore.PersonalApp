using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentHandlerTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresTheTrimmedStudentForTheSchool()
    {
        var school = Guid.NewGuid();

        var result = await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "  Anna ", " Svensson ", " 7B ", "  "), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FullName.ShouldBe("Anna Svensson");
        result.Value.ClassName.ShouldBe("7B");
        result.Value.Notes.ShouldBeNull();
        result.Value.ClientId.ShouldBe(school);
    }

    [Theory]
    [InlineData("", "Svensson", "student_first_name_required")]
    [InlineData("Anna", " ", "student_last_name_required")]
    public async Task CreateRejectsMissingNames(string first, string last, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), first, last, null, null), Ct);

        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreateRejectsTooLongFields()
    {
        var tooLongClass = await _fixture.Create.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), "Anna", "Svensson", new string('x', 101), null), Ct);
        var tooLongNotes = await _fixture.Create.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), "Anna", "Svensson", null, new string('x', 2001)), Ct);

        tooLongClass.Error.Code.ShouldBe("student_class_name_too_long");
        tooLongNotes.Error.Code.ShouldBe("student_notes_too_long");
    }

    [Fact]
    public async Task ListIsScopedToTheSchoolAndSortedByName()
    {
        var school = Guid.NewGuid();
        var other = Guid.NewGuid();
        await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "Zoe", "Berg", null, null), Ct);
        await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "Anna", "Cedar", null, null), Ct);
        await _fixture.Create.HandleAsync(new CreateStudent.Command(other, "Bo", "Dahl", null, null), Ct);

        var listed = await _fixture.List.HandleAsync(new ListStudents.Query(school), Ct);

        listed.Select(s => s.FullName).ShouldBe(["Zoe Berg", "Anna Cedar"]);
    }

    [Fact]
    public async Task UpdateChangesTheStudentAndReportsUnknownOnes()
    {
        var created = (await _fixture.Create.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), "Anna", "Svensson", null, null), Ct)).Value;

        var updated = await _fixture.Update.HandleAsync(new UpdateStudent.Command(created.Id, "Anna", "Berg", "8A", "Needs glasses"), Ct);
        var unknown = await _fixture.Update.HandleAsync(new UpdateStudent.Command(Guid.NewGuid(), "Anna", "Berg", null, null), Ct);

        updated.Value.LastName.ShouldBe("Berg");
        updated.Value.ClassName.ShouldBe("8A");
        updated.Value.ClientId.ShouldBe(created.ClientId);
        unknown.Error.Code.ShouldBe("student_not_found");
    }

    [Fact]
    public async Task DeleteRemovesTheStudentAndTellsTheOtherModules()
    {
        var school = Guid.NewGuid();
        var student = (await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "Anna", "Svensson", null, null), Ct)).Value;

        var result = await _fixture.Delete.HandleAsync(new DeleteStudent.Command(student.Id), Ct);
        var again = await _fixture.Delete.HandleAsync(new DeleteStudent.Command(student.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("student_not_found");
        _fixture.Events.Published.ShouldHaveSingleItem().ShouldBe(new StudentDeleted(student.Id));
        (await _fixture.List.HandleAsync(new ListStudents.Query(school), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingASchoolsStudentsLeavesOtherSchoolsAndAnnouncesEachOne()
    {
        var school = Guid.NewGuid();
        var other = Guid.NewGuid();
        var anna = (await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "Anna", "Svensson", null, null), Ct)).Value;
        var bo = (await _fixture.Create.HandleAsync(new CreateStudent.Command(school, "Bo", "Berg", null, null), Ct)).Value;
        await _fixture.Create.HandleAsync(new CreateStudent.Command(other, "Cia", "Dahl", null, null), Ct);

        var deleted = await _fixture.DeleteForClient.HandleAsync(new DeleteStudentsForClient.Command(school), Ct);

        deleted.ShouldBe(2);
        (await _fixture.List.HandleAsync(new ListStudents.Query(school), Ct)).ShouldBeEmpty();
        (await _fixture.List.HandleAsync(new ListStudents.Query(other), Ct)).ShouldHaveSingleItem();
        _fixture.Events.Published.ShouldBe([new StudentDeleted(anna.Id), new StudentDeleted(bo.Id)], ignoreOrder: true);
    }
}

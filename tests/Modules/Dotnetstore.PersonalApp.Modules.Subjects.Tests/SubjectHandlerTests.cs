using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

public sealed class SubjectHandlerTests : IDisposable
{
    private readonly SubjectsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateStoresTheTrimmedSubject()
    {
        var result = await _fixture.Create.HandleAsync(new CreateSubject.Command("  Mathematics ", "  "), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Mathematics");
        result.Value.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "subject_name_required")]
    [InlineData("   ", "subject_name_required")]
    public async Task CreateRejectsAMissingName(string name, string expectedCode)
    {
        var result = await _fixture.Create.HandleAsync(new CreateSubject.Command(name, null), Ct);

        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task NamesAreUniqueIgnoringCase()
    {
        await _fixture.CreateSubjectAsync("Mathematics", Ct);

        var duplicate = await _fixture.Create.HandleAsync(new CreateSubject.Command("mathematics", null), Ct);

        duplicate.Error.Code.ShouldBe("subject_conflict");
    }

    [Fact]
    public async Task ListIsSortedAndSearchable()
    {
        await _fixture.CreateSubjectAsync("Physics", Ct);
        await _fixture.CreateSubjectAsync("Biology", Ct);
        await _fixture.CreateSubjectAsync("Mathematics", Ct);

        var all = await _fixture.List.HandleAsync(new ListSubjects.Query(), Ct);
        var searched = await _fixture.List.HandleAsync(new ListSubjects.Query("phys"), Ct);

        all.Items.Select(s => s.Name).ShouldBe(["Biology", "Mathematics", "Physics"]);
        searched.Items.ShouldHaveSingleItem().Name.ShouldBe("Physics");
    }

    [Fact]
    public async Task UpdateRenamesAndRejectsClashesAndUnknownSubjects()
    {
        var math = await _fixture.CreateSubjectAsync("Math", Ct);
        await _fixture.CreateSubjectAsync("Physics", Ct);

        var renamed = await _fixture.Update.HandleAsync(new UpdateSubject.Command(math.Id, "Mathematics", "Numbers"), Ct);
        var clash = await _fixture.Update.HandleAsync(new UpdateSubject.Command(math.Id, "physics", null), Ct);
        var unknown = await _fixture.Update.HandleAsync(new UpdateSubject.Command(Guid.NewGuid(), "Art", null), Ct);

        renamed.Value.Name.ShouldBe("Mathematics");
        renamed.Value.Description.ShouldBe("Numbers");
        clash.Error.Code.ShouldBe("subject_conflict");
        unknown.Error.Code.ShouldBe("subject_not_found");
    }

    [Fact]
    public async Task DeleteRemovesTheSubjectAndTakesItOffTheTeachers()
    {
        var teacher = Guid.NewGuid();
        var math = await _fixture.CreateSubjectAsync("Mathematics", Ct);
        await _fixture.AddTaught.HandleAsync(new Features.TeacherSubjects.AddTeacherSubject.Command(teacher, math.Id), Ct);

        var result = await _fixture.Delete.HandleAsync(new DeleteSubject.Command(math.Id), Ct);
        var again = await _fixture.Delete.HandleAsync(new DeleteSubject.Command(math.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("subject_not_found");
        _fixture.Events.Published.ShouldBe([new SubjectDeleted(math.Id)]);
        (await _fixture.ListTaught.HandleAsync(new Features.TeacherSubjects.ListTeacherSubjects.Query(teacher), Ct)).ShouldBeEmpty();
    }
}

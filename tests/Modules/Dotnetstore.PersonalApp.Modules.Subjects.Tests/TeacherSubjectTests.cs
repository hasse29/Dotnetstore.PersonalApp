using Dotnetstore.PersonalApp.Modules.Subjects.Consumers;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

public sealed class TeacherSubjectTests : IDisposable
{
    private readonly SubjectsFixture _fixture = new();
    private readonly Guid _teacher = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ATeacherCanTeachSeveralSubjectsListedByName()
    {
        var physics = await _fixture.CreateSubjectAsync("Physics", Ct);
        var biology = await _fixture.CreateSubjectAsync("Biology", Ct);
        await _fixture.CreateSubjectAsync("Art", Ct);

        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, physics.Id), Ct);
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, biology.Id), Ct);

        var taught = await _fixture.ListTaught.HandleAsync(new ListTeacherSubjects.Query(_teacher), Ct);
        taught.Select(s => s.Name).ShouldBe(["Biology", "Physics"]);
    }

    [Fact]
    public async Task SeveralTeachersCanTeachTheSameSubject()
    {
        var math = await _fixture.CreateSubjectAsync("Mathematics", Ct);
        var other = Guid.NewGuid();

        (await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, math.Id), Ct)).IsSuccess.ShouldBeTrue();
        (await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(other, math.Id), Ct)).IsSuccess.ShouldBeTrue();

        (await _fixture.ListTaught.HandleAsync(new ListTeacherSubjects.Query(other), Ct)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AddingTwiceOrAnUnknownSubjectIsRejected()
    {
        var math = await _fixture.CreateSubjectAsync("Mathematics", Ct);
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, math.Id), Ct);

        var twice = await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, math.Id), Ct);
        var unknown = await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, Guid.NewGuid()), Ct);

        twice.Error.Code.ShouldBe("teacher_subject_conflict");
        unknown.Error.Code.ShouldBe("subject_not_found");
    }

    [Fact]
    public async Task RemoveStopsTeachingOnlyForThatTeacher()
    {
        var math = await _fixture.CreateSubjectAsync("Mathematics", Ct);
        var other = Guid.NewGuid();
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, math.Id), Ct);
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(other, math.Id), Ct);

        var removed = await _fixture.RemoveTaught.HandleAsync(new RemoveTeacherSubject.Command(_teacher, math.Id), Ct);
        var again = await _fixture.RemoveTaught.HandleAsync(new RemoveTeacherSubject.Command(_teacher, math.Id), Ct);

        removed.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("teacher_subject_not_found");
        (await _fixture.ListTaught.HandleAsync(new ListTeacherSubjects.Query(_teacher), Ct)).ShouldBeEmpty();
        (await _fixture.ListTaught.HandleAsync(new ListTeacherSubjects.Query(other), Ct)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ADeletedUserNoLongerTeachesAnything()
    {
        var math = await _fixture.CreateSubjectAsync("Mathematics", Ct);
        var other = Guid.NewGuid();
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, math.Id), Ct);
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(other, math.Id), Ct);

        await new UserDeletedConsumer(_fixture.DeleteForUser).ConsumeAsync(new UserDeleted(_teacher), Ct);

        (await _fixture.ListTaught.HandleAsync(new ListTeacherSubjects.Query(_teacher), Ct)).ShouldBeEmpty();
        (await _fixture.ListTaught.HandleAsync(new ListTeacherSubjects.Query(other), Ct)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task TheTabOffersOnlySubjectsNotTaughtYetAndAddsAndRemovesThem()
    {
        var math = await _fixture.CreateSubjectAsync("Mathematics", Ct);
        await _fixture.CreateSubjectAsync("Physics", Ct);
        await _fixture.AddTaught.HandleAsync(new AddTeacherSubject.Command(_teacher, math.Id), Ct);
        var panel = new UserSubjectsViewModel(_teacher, _fixture.ListTaught, _fixture.List, _fixture.AddTaught, _fixture.RemoveTaught, new Localizer());

        await panel.LoadCommand.ExecuteAsync(null);

        panel.Taught.Select(s => s.Name).ShouldBe(["Mathematics"]);
        panel.Available.Select(s => s.Name).ShouldBe(["Physics"]);
        panel.HasCatalogue.ShouldBeTrue();

        panel.SelectedAvailable = panel.Available[0];
        await panel.AddCommand.ExecuteAsync(null);
        panel.Taught.Select(s => s.Name).ShouldBe(["Mathematics", "Physics"]);
        panel.Available.ShouldBeEmpty();

        await panel.RemoveCommand.ExecuteAsync(panel.Taught[0]);
        panel.Taught.Select(s => s.Name).ShouldBe(["Physics"]);
        panel.Available.Select(s => s.Name).ShouldBe(["Mathematics"]);
    }
}

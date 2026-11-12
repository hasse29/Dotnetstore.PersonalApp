using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

public sealed class GetSubjectAndLookupTests : IDisposable
{
    private readonly SubjectsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetsASubjectById()
    {
        var created = await _fixture.CreateSubjectAsync("Mathematics", Ct);

        var result = await new GetSubject.Handler(_fixture.DbFactory).HandleAsync(new GetSubject.Query(created.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Mathematics");
    }

    [Fact]
    public async Task AnUnknownSubjectIsNotFound()
    {
        var result = await new GetSubject.Handler(_fixture.DbFactory).HandleAsync(new GetSubject.Query(Guid.NewGuid()), Ct);

        result.Error.Code.ShouldBe("subject_not_found");
    }

    [Fact]
    public async Task TheLookupListsSubjectsByNameAndKnowsWhichExist()
    {
        var physics = await _fixture.CreateSubjectAsync("Physics", Ct);
        await _fixture.CreateSubjectAsync("Art", Ct);
        var lookup = new SubjectLookup(_fixture.DbFactory);

        var all = await lookup.ListAsync(Ct);

        all.Select(s => s.Name).ShouldBe(["Art", "Physics"]);
        (await lookup.ExistsAsync(physics.Id, Ct)).ShouldBeTrue();
        (await lookup.ExistsAsync(Guid.NewGuid(), Ct)).ShouldBeFalse();
    }
}

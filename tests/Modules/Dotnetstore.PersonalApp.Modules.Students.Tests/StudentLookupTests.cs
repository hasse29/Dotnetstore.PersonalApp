using Dotnetstore.PersonalApp.Modules.Students.Features.Students;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentLookupTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<Guid> AddAsync(Guid school, string first, string last, string? className = null) =>
        (await _fixture.Create.HandleAsync(new CreateStudent.Command(school, first, last, className, null), Ct)).Value.Id;

    [Fact]
    public async Task ListsTheStudentsOfOneSchoolByLastNameThenFirstName()
    {
        var school = Guid.NewGuid();
        await AddAsync(school, "Bo", "Svensson", "7B");
        await AddAsync(school, "Anna", "Svensson");
        await AddAsync(school, "Carl", "Andersson");
        await AddAsync(Guid.NewGuid(), "Other", "School");

        var students = await new StudentLookup(_fixture.DbFactory).ListForSchoolAsync(school, Ct);

        students.Select(s => s.Name).ShouldBe(["Carl Andersson", "Anna Svensson", "Bo Svensson"]);
        students.ShouldAllBe(s => s.ClientId == school);
        students.Single(s => s.Name == "Bo Svensson").ClassName.ShouldBe("7B");
    }

    [Fact]
    public async Task ListsEveryStudentAndKnowsWhichExist()
    {
        var anna = await AddAsync(Guid.NewGuid(), "Anna", "Svensson");
        await AddAsync(Guid.NewGuid(), "Bo", "Andersson");
        var lookup = new StudentLookup(_fixture.DbFactory);

        (await lookup.ListAsync(Ct)).Select(s => s.Name).ShouldBe(["Bo Andersson", "Anna Svensson"]);
        (await lookup.ExistsAsync(anna, Ct)).ShouldBeTrue();
        (await lookup.ExistsAsync(Guid.NewGuid(), Ct)).ShouldBeFalse();
    }
}

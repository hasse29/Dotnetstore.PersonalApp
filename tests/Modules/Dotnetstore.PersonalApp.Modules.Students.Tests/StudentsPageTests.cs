using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentsPageTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();
    private readonly ClientReference _north = new(Guid.NewGuid(), "North School");
    private readonly ClientReference _south = new(Guid.NewGuid(), "South School");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private FakeClients Clients => new(_north, _south);

    private CreateStudent.Handler CreateChecked => new(_fixture.DbFactory, new CreateStudent.Validator(), _fixture.Clock, Clients);

    private StudentsViewModel BuildPage() =>
        new(null, _fixture.List, CreateChecked, _fixture.Update, _fixture.Delete, new Localizer(), clients: Clients);

    [Fact]
    public async Task CreateRejectsASchoolThatDoesNotExist()
    {
        var result = await CreateChecked.HandleAsync(new CreateStudent.Command(Guid.NewGuid(), "Anna", "Svensson", null, null), Ct);

        result.Error.Code.ShouldBe("student_school_not_found");
        (await _fixture.List.HandleAsync(new ListStudents.Query(null), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAcceptsAnExistingSchool()
    {
        var result = await CreateChecked.HandleAsync(new CreateStudent.Command(_north.Id, "Anna", "Svensson", null, null), Ct);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ThePageListsEverySchoolsStudentsWithSchoolNames()
    {
        await CreateChecked.HandleAsync(new CreateStudent.Command(_north.Id, "Anna", "Svensson", null, null), Ct);
        await CreateChecked.HandleAsync(new CreateStudent.Command(_south.Id, "Bo", "Berg", null, null), Ct);
        var page = BuildPage();

        await page.LoadCommand.ExecuteAsync(null);

        page.IsPageLayout.ShouldBeTrue();
        page.Students.Select(s => (s.FullName, s.SchoolName)).ShouldBe([("Bo Berg", "South School"), ("Anna Svensson", "North School")], ignoreOrder: true);
        page.Schools.Select(s => s.Name).Skip(1).ShouldBe(["North School", "South School"]);
    }

    [Fact]
    public async Task ChoosingASchoolFiltersTheList()
    {
        await CreateChecked.HandleAsync(new CreateStudent.Command(_north.Id, "Anna", "Svensson", null, null), Ct);
        await CreateChecked.HandleAsync(new CreateStudent.Command(_south.Id, "Bo", "Berg", null, null), Ct);
        var page = BuildPage();
        await page.LoadCommand.ExecuteAsync(null);

        page.SelectedSchoolFilter = page.Schools.Single(s => s.Id == _south.Id);
        await Task.Delay(200, Ct);

        page.Students.ShouldHaveSingleItem().FullName.ShouldBe("Bo Berg");
    }

    [Fact]
    public async Task TheEditorOnThePageNeedsASchool()
    {
        var page = BuildPage();
        await page.LoadCommand.ExecuteAsync(null);
        page.NewCommand.Execute(null);
        var editor = page.Editor.ShouldNotBeNull();
        editor.ShowSchoolPicker.ShouldBeTrue();
        editor.FirstName = "Anna";
        editor.LastName = "Svensson";

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldNotBeNullOrEmpty();

        editor.SelectedSchool = editor.Schools.Single(s => s.Id == _north.Id);
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        page.Students.ShouldHaveSingleItem().SchoolName.ShouldBe("North School");
    }

    private sealed class FakeClients(params ClientReference[] clients) : IClientLookup
    {
        public Task<IReadOnlyList<ClientReference>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ClientReference>>([.. clients.OrderBy(c => c.Name)]);
    }
}

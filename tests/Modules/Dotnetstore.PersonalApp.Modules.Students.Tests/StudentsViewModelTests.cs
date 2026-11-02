using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentsViewModelTests : IDisposable
{
    private readonly StudentsFixture _fixture = new();
    private readonly Guid _school = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private StudentsViewModel Build() =>
        new(_school, _fixture.List, _fixture.Create, _fixture.Update, _fixture.Delete, new Localizer());

    [Fact]
    public async Task LoadListsTheSchoolsStudents()
    {
        await _fixture.Create.HandleAsync(new CreateStudent.Command(_school, "Anna", "Svensson", "7B", null), Ct);
        var viewModel = Build();

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.Students.ShouldHaveSingleItem().FullName.ShouldBe("Anna Svensson");
    }

    [Fact]
    public async Task SavingTheEditorAddsTheStudentAndClosesIt()
    {
        var viewModel = Build();
        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldNotBeNull();
        editor.FirstName = "Anna";
        editor.LastName = "Svensson";

        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        viewModel.Editor.ShouldBeNull();
        viewModel.Students.ShouldHaveSingleItem().LastName.ShouldBe("Svensson");
    }

    [Fact]
    public async Task AnInvalidEditorShowsTheErrorAndStaysOpen()
    {
        var viewModel = Build();
        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldNotBeNull();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldNotBeNullOrEmpty();
        viewModel.Editor.ShouldBeSameAs(editor);
    }

    [Fact]
    public async Task DeletingTheSelectedStudentRemovesItFromTheList()
    {
        await _fixture.Create.HandleAsync(new CreateStudent.Command(_school, "Anna", "Svensson", null, null), Ct);
        var viewModel = Build();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedStudent = viewModel.Students[0];
        viewModel.Editor = null;

        viewModel.RequestDeleteCommand.Execute(null);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.Students.ShouldBeEmpty();
    }
}

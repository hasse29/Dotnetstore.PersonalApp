using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvDetailViewModelTests : IDisposable
{
    private readonly CvsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<CvDetailViewModel> OpenAsync()
    {
        _fixture.Users.User = new UserReference(_user, "Anna", "Berg", "anna", new DateOnly(1990, 5, 17));
        var cv = (await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct)).Value;
        var viewModel = new CvDetailViewModel(cv, _user, _fixture.DetailServices);
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task LoadShowsTheUsersDetailsAndTheCvsSummary()
    {
        var viewModel = await OpenAsync();

        viewModel.FullName.ShouldBe("Anna Berg");
        viewModel.Initials.ShouldBe("AB");
        viewModel.DateOfBirth.ShouldBe("1990-05-17");
        viewModel.Summary.ShouldBe("Ten years of experience.");
        viewModel.DriverLicence.ShouldBe("No driver licence");
        viewModel.HasNoPhoto.ShouldBeTrue();
        viewModel.Sections.ShouldAllBe(s => s.IsEmpty);
    }

    [Fact]
    public async Task AddingAJobThroughTheEditorShowsItOnThePage()
    {
        var viewModel = await OpenAsync();

        viewModel.Work.AddCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<PeriodEditorViewModel>();
        editor.Organisation = "Acme";
        editor.Position = "Developer";
        editor.StartDate = new DateTime(2020, 1, 1);
        editor.IsOngoing = true;
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        viewModel.Editor.ShouldBeNull();
        var item = viewModel.Work.Items.ShouldHaveSingleItem();
        item.Title.ShouldBe("Developer");
        item.Subtitle.ShouldBe("Acme");
        item.Meta.ShouldBe("2020-01 – present");
    }

    [Fact]
    public async Task EditorShowsTheTranslatedErrorAndKeepsTheUserInTheEditor()
    {
        var viewModel = await OpenAsync();
        viewModel.Work.AddCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<PeriodEditorViewModel>();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Start date is required.");
        viewModel.Editor.ShouldBeSameAs(editor);
    }

    [Fact]
    public async Task EditingAnEntryAllowsDeletingItAfterConfirmation()
    {
        var viewModel = await OpenAsync();
        viewModel.Languages.AddCommand.Execute(null);
        var add = viewModel.Editor.ShouldBeOfType<LanguageEditorViewModel>();
        add.Language = "English";
        await add.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        viewModel.Languages.Items[0].EditCommand.Execute(null);
        var edit = viewModel.Editor.ShouldBeOfType<LanguageEditorViewModel>();
        edit.CanDelete.ShouldBeTrue();
        edit.RequestDeleteCommand.Execute(null);
        await edit.ConfirmDeleteCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        viewModel.Editor.ShouldBeNull();
        viewModel.Languages.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task DriverLicenceEditorSavesTheTickedCategories()
    {
        var viewModel = await OpenAsync();

        viewModel.EditDriverLicenceCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<DriverLicenceEditorViewModel>();
        editor.Options.Single(o => o.Code == "B").IsSelected = true;
        editor.Options.Single(o => o.Code == "BE").IsSelected = true;
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        viewModel.DriverLicence.ShouldBe("B, BE");
    }

    [Fact]
    public async Task ChoosingAPhotoStoresItAndRemovingClearsIt()
    {
        var viewModel = await OpenAsync();
        _fixture.FileOpen.Next = new PickedFile("me.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 });

        await viewModel.ChoosePhotoCommand.ExecuteAsync(null);
        viewModel.HasNoPhoto.ShouldBeFalse();

        await viewModel.RemovePhotoCommand.ExecuteAsync(null);
        viewModel.HasNoPhoto.ShouldBeTrue();
    }

    [Fact]
    public async Task CancellingThePhotoDialogLeavesThePhotoAlone()
    {
        var viewModel = await OpenAsync();
        _fixture.FileOpen.Next = null;

        await viewModel.ChoosePhotoCommand.ExecuteAsync(null);

        viewModel.HasNoPhoto.ShouldBeTrue();
        viewModel.StatusMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task TechnologiesOfAllJobsAreSummarisedWithMostUsedFirst()
    {
        var viewModel = await OpenAsync();
        await AddJobAsync(viewModel, "Acme", "C#, SQL");
        await AddJobAsync(viewModel, "Globex", "c#, Docker");

        viewModel.Technologies.Select(t => t.Text).ShouldBe(["C# ×2", "Docker", "SQL"]);
        viewModel.HasNoTechnologies.ShouldBeFalse();
        viewModel.Work.Items.Single(i => i.Subtitle == "Acme").Tags.ShouldBe(["C#", "SQL"]);
    }

    [Fact]
    public async Task OnlyAJobHasATechnologiesField()
    {
        var viewModel = await OpenAsync();

        viewModel.Work.AddCommand.Execute(null);
        (viewModel.Editor.ShouldBeOfType<PeriodEditorViewModel>().ShowTechnologies).ShouldBeTrue();

        viewModel.Education.AddCommand.Execute(null);
        (viewModel.Editor.ShouldBeOfType<PeriodEditorViewModel>().ShowTechnologies).ShouldBeFalse();
    }

    private static async Task AddJobAsync(CvDetailViewModel viewModel, string employer, string technologies)
    {
        viewModel.Work.AddCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<PeriodEditorViewModel>();
        editor.Organisation = employer;
        editor.Position = "Developer";
        editor.StartDate = new DateTime(2020, 1, 1);
        editor.IsOngoing = true;
        editor.Technologies = technologies;
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, TestContext.Current.CancellationToken);
    }
}

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

        Assert.Equal("Anna Berg", viewModel.FullName);
        Assert.Equal("AB", viewModel.Initials);
        Assert.Equal("1990-05-17", viewModel.DateOfBirth);
        Assert.Equal("Ten years of experience.", viewModel.Summary);
        Assert.Equal("No driver licence", viewModel.DriverLicence);
        Assert.True(viewModel.HasNoPhoto);
        Assert.All(viewModel.Sections, s => Assert.True(s.IsEmpty));
    }

    [Fact]
    public async Task AddingAJobThroughTheEditorShowsItOnThePage()
    {
        var viewModel = await OpenAsync();

        viewModel.Work.AddCommand.Execute(null);
        var editor = Assert.IsType<PeriodEditorViewModel>(viewModel.Editor);
        editor.Organisation = "Acme";
        editor.Position = "Developer";
        editor.StartDate = new DateTime(2020, 1, 1);
        editor.IsOngoing = true;
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        Assert.Null(viewModel.Editor);
        var item = Assert.Single(viewModel.Work.Items);
        Assert.Equal("Developer", item.Title);
        Assert.Equal("Acme", item.Subtitle);
        Assert.Equal("2020-01 – present", item.Meta);
    }

    [Fact]
    public async Task EditorShowsTheTranslatedErrorAndKeepsTheUserInTheEditor()
    {
        var viewModel = await OpenAsync();
        viewModel.Work.AddCommand.Execute(null);
        var editor = Assert.IsType<PeriodEditorViewModel>(viewModel.Editor);

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Start date is required.", editor.ErrorMessage);
        Assert.Same(editor, viewModel.Editor);
    }

    [Fact]
    public async Task EditingAnEntryAllowsDeletingItAfterConfirmation()
    {
        var viewModel = await OpenAsync();
        viewModel.Languages.AddCommand.Execute(null);
        var add = Assert.IsType<LanguageEditorViewModel>(viewModel.Editor);
        add.Language = "English";
        await add.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        viewModel.Languages.Items[0].EditCommand.Execute(null);
        var edit = Assert.IsType<LanguageEditorViewModel>(viewModel.Editor);
        Assert.True(edit.CanDelete);
        edit.RequestDeleteCommand.Execute(null);
        await edit.ConfirmDeleteCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        Assert.Null(viewModel.Editor);
        Assert.Empty(viewModel.Languages.Items);
    }

    [Fact]
    public async Task DriverLicenceEditorSavesTheTickedCategories()
    {
        var viewModel = await OpenAsync();

        viewModel.EditDriverLicenceCommand.Execute(null);
        var editor = Assert.IsType<DriverLicenceEditorViewModel>(viewModel.Editor);
        editor.Options.Single(o => o.Code == "B").IsSelected = true;
        editor.Options.Single(o => o.Code == "BE").IsSelected = true;
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, Ct);

        Assert.Equal("B, BE", viewModel.DriverLicence);
    }

    [Fact]
    public async Task ChoosingAPhotoStoresItAndRemovingClearsIt()
    {
        var viewModel = await OpenAsync();
        _fixture.FileOpen.Next = new PickedFile("me.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 });

        await viewModel.ChoosePhotoCommand.ExecuteAsync(null);
        Assert.False(viewModel.HasNoPhoto);

        await viewModel.RemovePhotoCommand.ExecuteAsync(null);
        Assert.True(viewModel.HasNoPhoto);
    }

    [Fact]
    public async Task CancellingThePhotoDialogLeavesThePhotoAlone()
    {
        var viewModel = await OpenAsync();
        _fixture.FileOpen.Next = null;

        await viewModel.ChoosePhotoCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasNoPhoto);
        Assert.NotNull(viewModel.StatusMessage);
    }

    [Fact]
    public async Task TechnologiesOfAllJobsAreSummarisedWithMostUsedFirst()
    {
        var viewModel = await OpenAsync();
        await AddJobAsync(viewModel, "Acme", "C#, SQL");
        await AddJobAsync(viewModel, "Globex", "c#, Docker");

        Assert.Equal(["C# ×2", "Docker", "SQL"], viewModel.Technologies.Select(t => t.Text));
        Assert.False(viewModel.HasNoTechnologies);
        Assert.Equal(["C#", "SQL"], viewModel.Work.Items.Single(i => i.Subtitle == "Acme").Tags);
    }

    [Fact]
    public async Task OnlyAJobHasATechnologiesField()
    {
        var viewModel = await OpenAsync();

        viewModel.Work.AddCommand.Execute(null);
        Assert.True(Assert.IsType<PeriodEditorViewModel>(viewModel.Editor).ShowTechnologies);

        viewModel.Education.AddCommand.Execute(null);
        Assert.False(Assert.IsType<PeriodEditorViewModel>(viewModel.Editor).ShowTechnologies);
    }

    private static async Task AddJobAsync(CvDetailViewModel viewModel, string employer, string technologies)
    {
        viewModel.Work.AddCommand.Execute(null);
        var editor = Assert.IsType<PeriodEditorViewModel>(viewModel.Editor);
        editor.Organisation = employer;
        editor.Position = "Developer";
        editor.StartDate = new DateTime(2020, 1, 1);
        editor.IsOngoing = true;
        editor.Technologies = technologies;
        await editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(200, TestContext.Current.CancellationToken);
    }
}

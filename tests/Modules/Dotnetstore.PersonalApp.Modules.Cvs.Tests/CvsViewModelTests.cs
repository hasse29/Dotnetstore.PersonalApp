using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Modules.Cvs.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvsViewModelTests : IDisposable
{
    private readonly CvsFixture _fixture = new();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public CvsViewModelTests()
    {
        _localization.AddResources(CvsStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private CvsViewModel CreateViewModel(Guid? userId = null, bool isPageLayout = true) =>
        new(userId ?? _user, isPageLayout, _fixture.List, _fixture.Create, _fixture.Update, _fixture.Delete, _fixture.DetailServices, _localization);

    [Fact]
    public async Task LoadShowsOnlyTheUsersCvs()
    {
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct);
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(Guid.NewGuid()), Ct);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Cvs);
        Assert.Equal("1 CV(s)", viewModel.StatusMessage);
    }

    [Fact]
    public async Task NewThenSaveAddsTheCvAndClosesTheEditor()
    {
        var viewModel = CreateViewModel();
        viewModel.NewCommand.Execute(null);
        Assert.NotNull(viewModel.Editor);

        viewModel.Editor.Title = "Data engineer";
        await viewModel.Editor.SaveCommand.ExecuteAsync(null);
        await Task.Delay(100, Ct);

        Assert.Null(viewModel.Editor);
        Assert.Equal("Data engineer", Assert.Single(viewModel.Cvs).Title);
    }

    [Fact]
    public async Task SaveWithoutATitleShowsTheTranslatedError()
    {
        var viewModel = CreateViewModel();
        viewModel.NewCommand.Execute(null);

        await viewModel.Editor!.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Title is required.", viewModel.Editor.ErrorMessage);
    }

    [Fact]
    public async Task ConfirmedDeleteRemovesTheSelectedCv()
    {
        await _fixture.Create.HandleAsync(CvsFixture.ValidCv(_user), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedCv = viewModel.Cvs[0];

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDelete);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Cvs);
        Assert.Null(viewModel.Editor);
    }
}

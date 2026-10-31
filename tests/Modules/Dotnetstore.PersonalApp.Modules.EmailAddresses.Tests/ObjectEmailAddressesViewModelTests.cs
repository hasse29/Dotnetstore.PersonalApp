using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Tests;

public sealed class ObjectEmailAddressesViewModelTests : IDisposable
{
    private readonly EmailAddressesFixture _fixture = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public ObjectEmailAddressesViewModelTests()
    {
        _localization.AddResources(EmailAddressesStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private ObjectEmailAddressesViewModel CreateViewModel(Guid? objectId = null) => new(
        objectId ?? _owner,
        _fixture.ListEmailAddresses,
        _fixture.List,
        _fixture.CreateEmailAddress,
        _fixture.UpdateEmailAddress,
        _fixture.DeleteEmailAddress,
        _fixture.DeleteEmailAddressesForObject,
        _localization);

    [Fact]
    public async Task LoadShowsOnlyTheObjectsEmailAddresses()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(Guid.NewGuid()), Ct);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Single(viewModel.EmailAddresses);
        Assert.True(viewModel.HasEmailAddresses);
        Assert.Equal("1 email address(es)", viewModel.StatusMessage);
    }

    [Fact]
    public async Task NewOpensAnEditorThatCreatesTheEmailAddressForTheObject()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = Assert.IsType<EmailAddressEditorViewModel>(viewModel.Editor);
        editor.Email = "ada@example.com";
        await editor.SaveCommand.ExecuteAsync(null);

        var address = Assert.Single(viewModel.EmailAddresses);
        Assert.Equal(_owner, address.ObjectId);
        Assert.Null(viewModel.Editor);
    }

    [Fact]
    public async Task SaveShowsATranslatedErrorAndKeepsTheEditorOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        var editor = Assert.IsType<EmailAddressEditorViewModel>(viewModel.Editor);

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Email address is required.", editor.ErrorMessage);
        Assert.NotNull(viewModel.Editor);
    }

    [Fact]
    public async Task SelectingAnEmailAddressOpensItsEditorWithItsType()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, _fixture.WorkTypeId), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedEmailAddress = viewModel.EmailAddresses[0];

        var editor = Assert.IsType<EmailAddressEditorViewModel>(viewModel.Editor);
        Assert.Equal("Work", editor.SelectedType?.Name);
        Assert.Equal("ada@example.com", editor.Email);
    }

    [Fact]
    public async Task DeleteAsksForConfirmationAndRemovesOnlyTheSelectedEmailAddress()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "lund@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "malmo@example.com"), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedEmailAddress = viewModel.EmailAddresses[0];

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDelete);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Single(viewModel.EmailAddresses);
        Assert.True(viewModel.IsIdle);
    }

    [Fact]
    public async Task DeleteAllRemovesEveryEmailAddressOfTheObjectButNotOthers()
    {
        var other = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "lund@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, email: "malmo@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(other), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDeleteAll);
        await viewModel.ConfirmDeleteAllCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.EmailAddresses);
        Assert.False(viewModel.HasEmailAddresses);
        Assert.Equal("Deleted 2 email address(es).", viewModel.StatusMessage);
        var remaining = await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(other), Ct);
        Assert.Single(remaining.Items);
    }

    [Fact]
    public async Task CancelLeavesTheConfirmation()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        Assert.True(viewModel.IsIdle);
        Assert.Single(viewModel.EmailAddresses);
    }
}

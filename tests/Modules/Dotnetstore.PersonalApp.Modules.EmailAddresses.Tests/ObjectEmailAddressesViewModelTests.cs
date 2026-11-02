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

        viewModel.EmailAddresses.ShouldHaveSingleItem();
        viewModel.HasEmailAddresses.ShouldBeTrue();
        viewModel.StatusMessage.ShouldBe("1 email address(es)");
    }

    [Fact]
    public async Task NewOpensAnEditorThatCreatesTheEmailAddressForTheObject()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<EmailAddressEditorViewModel>();
        editor.Email = "ada@example.com";
        await editor.SaveCommand.ExecuteAsync(null);

        var address = viewModel.EmailAddresses.ShouldHaveSingleItem();
        address.ObjectId.ShouldBe(_owner);
        viewModel.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SaveShowsATranslatedErrorAndKeepsTheEditorOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<EmailAddressEditorViewModel>();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Email address is required.");
        viewModel.Editor.ShouldNotBeNull();
    }

    [Fact]
    public async Task SelectingAnEmailAddressOpensItsEditorWithItsType()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner, _fixture.WorkTypeId), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedEmailAddress = viewModel.EmailAddresses[0];

        var editor = viewModel.Editor.ShouldBeOfType<EmailAddressEditorViewModel>();
        (editor.SelectedType?.Name).ShouldBe("Work");
        editor.Email.ShouldBe("ada@example.com");
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
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.EmailAddresses.ShouldHaveSingleItem();
        viewModel.IsIdle.ShouldBeTrue();
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
        viewModel.IsConfirmingDeleteAll.ShouldBeTrue();
        await viewModel.ConfirmDeleteAllCommand.ExecuteAsync(null);

        viewModel.EmailAddresses.ShouldBeEmpty();
        viewModel.HasEmailAddresses.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Deleted 2 email address(es).");
        var remaining = await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(other), Ct);
        remaining.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CancelLeavesTheConfirmation()
    {
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(_owner), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        viewModel.IsIdle.ShouldBeTrue();
        viewModel.EmailAddresses.ShouldHaveSingleItem();
    }
}

using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Tests;

public sealed class ObjectPhoneNumbersViewModelTests : IDisposable
{
    private readonly PhoneNumbersFixture _fixture = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public ObjectPhoneNumbersViewModelTests()
    {
        _localization.AddResources(PhoneNumbersStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private ObjectPhoneNumbersViewModel CreateViewModel(Guid? objectId = null) => new(
        objectId ?? _owner,
        _fixture.ListPhoneNumbers,
        _fixture.List,
        _fixture.CreatePhoneNumber,
        _fixture.UpdatePhoneNumber,
        _fixture.DeletePhoneNumber,
        _fixture.DeletePhoneNumbersForObject,
        _localization);

    [Fact]
    public async Task LoadShowsOnlyTheObjectsPhoneNumbers()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(Guid.NewGuid()), Ct);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.PhoneNumbers.ShouldHaveSingleItem();
        viewModel.HasPhoneNumbers.ShouldBeTrue();
        viewModel.StatusMessage.ShouldBe("1 phone number(s)");
    }

    [Fact]
    public async Task NewOpensAnEditorThatCreatesThePhoneNumberForTheObject()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<PhoneNumberEditorViewModel>();
        editor.Number = "+46 8 123 456";
        await editor.SaveCommand.ExecuteAsync(null);

        var address = viewModel.PhoneNumbers.ShouldHaveSingleItem();
        address.ObjectId.ShouldBe(_owner);
        viewModel.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SaveShowsATranslatedErrorAndKeepsTheEditorOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<PhoneNumberEditorViewModel>();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Phone number is required.");
        viewModel.Editor.ShouldNotBeNull();
    }

    [Fact]
    public async Task SelectingAnPhoneNumberOpensItsEditorWithItsType()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, _fixture.WorkTypeId), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedPhoneNumber = viewModel.PhoneNumbers[0];

        var editor = viewModel.Editor.ShouldBeOfType<PhoneNumberEditorViewModel>();
        (editor.SelectedType?.Name).ShouldBe("Work");
        editor.Number.ShouldBe("+46 8 123 456");
    }

    [Fact]
    public async Task DeleteAsksForConfirmationAndRemovesOnlyTheSelectedPhoneNumber()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "+46 46 111 111"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "+46 40 222 222"), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedPhoneNumber = viewModel.PhoneNumbers[0];

        viewModel.RequestDeleteCommand.Execute(null);
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.PhoneNumbers.ShouldHaveSingleItem();
        viewModel.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAllRemovesEveryPhoneNumberOfTheObjectButNotOthers()
    {
        var other = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "+46 46 111 111"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, number: "+46 40 222 222"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(other), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.IsConfirmingDeleteAll.ShouldBeTrue();
        await viewModel.ConfirmDeleteAllCommand.ExecuteAsync(null);

        viewModel.PhoneNumbers.ShouldBeEmpty();
        viewModel.HasPhoneNumbers.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Deleted 2 phone number(s).");
        var remaining = await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(other), Ct);
        remaining.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CancelLeavesTheConfirmation()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        viewModel.IsIdle.ShouldBeTrue();
        viewModel.PhoneNumbers.ShouldHaveSingleItem();
    }
}

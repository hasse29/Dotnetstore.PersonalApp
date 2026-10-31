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

        Assert.Single(viewModel.PhoneNumbers);
        Assert.True(viewModel.HasPhoneNumbers);
        Assert.Equal("1 phone number(s)", viewModel.StatusMessage);
    }

    [Fact]
    public async Task NewOpensAnEditorThatCreatesThePhoneNumberForTheObject()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = Assert.IsType<PhoneNumberEditorViewModel>(viewModel.Editor);
        editor.Number = "+46 8 123 456";
        await editor.SaveCommand.ExecuteAsync(null);

        var address = Assert.Single(viewModel.PhoneNumbers);
        Assert.Equal(_owner, address.ObjectId);
        Assert.Null(viewModel.Editor);
    }

    [Fact]
    public async Task SaveShowsATranslatedErrorAndKeepsTheEditorOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        var editor = Assert.IsType<PhoneNumberEditorViewModel>(viewModel.Editor);

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Phone number is required.", editor.ErrorMessage);
        Assert.NotNull(viewModel.Editor);
    }

    [Fact]
    public async Task SelectingAnPhoneNumberOpensItsEditorWithItsType()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner, _fixture.WorkTypeId), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedPhoneNumber = viewModel.PhoneNumbers[0];

        var editor = Assert.IsType<PhoneNumberEditorViewModel>(viewModel.Editor);
        Assert.Equal("Work", editor.SelectedType?.Name);
        Assert.Equal("+46 8 123 456", editor.Number);
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
        Assert.True(viewModel.IsConfirmingDelete);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Single(viewModel.PhoneNumbers);
        Assert.True(viewModel.IsIdle);
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
        Assert.True(viewModel.IsConfirmingDeleteAll);
        await viewModel.ConfirmDeleteAllCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.PhoneNumbers);
        Assert.False(viewModel.HasPhoneNumbers);
        Assert.Equal("Deleted 2 phone number(s).", viewModel.StatusMessage);
        var remaining = await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(other), Ct);
        Assert.Single(remaining.Items);
    }

    [Fact]
    public async Task CancelLeavesTheConfirmation()
    {
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(_owner), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        Assert.True(viewModel.IsIdle);
        Assert.Single(viewModel.PhoneNumbers);
    }
}

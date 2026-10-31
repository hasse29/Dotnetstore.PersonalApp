using Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Addresses.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Tests;

public sealed class ObjectAddressesViewModelTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Localizer _localization = new();

    public ObjectAddressesViewModelTests()
    {
        _localization.AddResources(AddressesStrings.ResourceManager);
        _localization.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private ObjectAddressesViewModel CreateViewModel(Guid? objectId = null) => new(
        objectId ?? _owner,
        _fixture.ListAddresses,
        _fixture.List,
        _fixture.CreateAddress,
        _fixture.UpdateAddress,
        _fixture.DeleteAddress,
        _fixture.DeleteAddressesForObject,
        _localization);

    [Fact]
    public async Task LoadShowsOnlyTheObjectsAddresses()
    {
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(Guid.NewGuid()), Ct);
        var viewModel = CreateViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Addresses);
        Assert.True(viewModel.HasAddresses);
        Assert.Equal("1 address(es)", viewModel.StatusMessage);
    }

    [Fact]
    public async Task NewOpensAnEditorThatCreatesTheAddressForTheObject()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = Assert.IsType<AddressEditorViewModel>(viewModel.Editor);
        editor.Street1 = "Storgatan 1";
        editor.PostalCode = "111 22";
        editor.City = "Lund";
        editor.Country = "Sweden";
        await editor.SaveCommand.ExecuteAsync(null);

        var address = Assert.Single(viewModel.Addresses);
        Assert.Equal(_owner, address.ObjectId);
        Assert.Null(viewModel.Editor);
    }

    [Fact]
    public async Task SaveShowsATranslatedErrorAndKeepsTheEditorOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        var editor = Assert.IsType<AddressEditorViewModel>(viewModel.Editor);

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Street is required.", editor.ErrorMessage);
        Assert.NotNull(viewModel.Editor);
    }

    [Fact]
    public async Task SelectingAnAddressOpensItsEditorWithItsType()
    {
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, _fixture.DeliveryTypeId), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedAddress = viewModel.Addresses[0];

        var editor = Assert.IsType<AddressEditorViewModel>(viewModel.Editor);
        Assert.Equal("Delivery", editor.SelectedType?.Name);
        Assert.Equal("Drottninggatan 1", editor.Street1);
    }

    [Fact]
    public async Task DeleteAsksForConfirmationAndRemovesOnlyTheSelectedAddress()
    {
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Lund"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Malmö"), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedAddress = viewModel.Addresses[0];

        viewModel.RequestDeleteCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDelete);
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Addresses);
        Assert.True(viewModel.IsIdle);
    }

    [Fact]
    public async Task DeleteAllRemovesEveryAddressOfTheObjectButNotOthers()
    {
        var other = Guid.NewGuid();
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Lund"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, city: "Malmö"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(other), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        Assert.True(viewModel.IsConfirmingDeleteAll);
        await viewModel.ConfirmDeleteAllCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Addresses);
        Assert.False(viewModel.HasAddresses);
        Assert.Equal("Deleted 2 address(es).", viewModel.StatusMessage);
        var remaining = await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(other), Ct);
        Assert.Single(remaining.Items);
    }

    [Fact]
    public async Task CancelLeavesTheConfirmation()
    {
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        Assert.True(viewModel.IsIdle);
        Assert.Single(viewModel.Addresses);
    }
}

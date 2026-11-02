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

        viewModel.Addresses.ShouldHaveSingleItem();
        viewModel.HasAddresses.ShouldBeTrue();
        viewModel.StatusMessage.ShouldBe("1 address(es)");
    }

    [Fact]
    public async Task NewOpensAnEditorThatCreatesTheAddressForTheObject()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<AddressEditorViewModel>();
        editor.Street1 = "Storgatan 1";
        editor.PostalCode = "111 22";
        editor.City = "Lund";
        editor.Country = "Sweden";
        await editor.SaveCommand.ExecuteAsync(null);

        var address = viewModel.Addresses.ShouldHaveSingleItem();
        address.ObjectId.ShouldBe(_owner);
        viewModel.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SaveShowsATranslatedErrorAndKeepsTheEditorOpen()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.NewCommand.Execute(null);
        var editor = viewModel.Editor.ShouldBeOfType<AddressEditorViewModel>();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe("Street is required.");
        viewModel.Editor.ShouldNotBeNull();
    }

    [Fact]
    public async Task SelectingAnAddressOpensItsEditorWithItsType()
    {
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner, _fixture.DeliveryTypeId), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedAddress = viewModel.Addresses[0];

        var editor = viewModel.Editor.ShouldBeOfType<AddressEditorViewModel>();
        (editor.SelectedType?.Name).ShouldBe("Delivery");
        editor.Street1.ShouldBe("Drottninggatan 1");
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
        viewModel.IsConfirmingDelete.ShouldBeTrue();
        await viewModel.ConfirmDeleteCommand.ExecuteAsync(null);

        viewModel.Addresses.ShouldHaveSingleItem();
        viewModel.IsIdle.ShouldBeTrue();
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
        viewModel.IsConfirmingDeleteAll.ShouldBeTrue();
        await viewModel.ConfirmDeleteAllCommand.ExecuteAsync(null);

        viewModel.Addresses.ShouldBeEmpty();
        viewModel.HasAddresses.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Deleted 2 address(es).");
        var remaining = await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(other), Ct);
        remaining.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CancelLeavesTheConfirmation()
    {
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(_owner), Ct);
        var viewModel = CreateViewModel();
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.RequestDeleteAllCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        viewModel.IsIdle.ShouldBeTrue();
        viewModel.Addresses.ShouldHaveSingleItem();
    }
}

using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class UserDetailAddressesTabTests
{
    private sealed class FakeAddressesViewModel(Guid objectId) : ViewModelBase
    {
        public Guid ObjectId { get; } = objectId;
    }

    private sealed class FakeFactory : IObjectAddressesPanelFactory
    {
        public ViewModelBase Create(Guid objectId) => new FakeAddressesViewModel(objectId);
    }

    private sealed class FakeEmailFactory : IObjectEmailAddressesPanelFactory
    {
        public ViewModelBase Create(Guid objectId) => new FakeAddressesViewModel(objectId);
    }

    private sealed class FakePhoneFactory : IObjectPhoneNumbersPanelFactory
    {
        public ViewModelBase Create(Guid objectId) => new FakeAddressesViewModel(objectId);
    }

    private static UserDto Ada() =>
        new(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", new DateOnly(1815, 12, 10), DateTimeOffset.UnixEpoch);

    [Fact]
    public void AddressesTabIsCreatedForTheUsersId()
    {
        var user = Ada();

        var viewModel = new UserDetailViewModel(user, new FakeFactory());

        var addresses = Assert.IsType<FakeAddressesViewModel>(viewModel.Addresses);
        Assert.Equal(user.Id, addresses.ObjectId);
    }

    [Fact]
    public void EmailAddressesTabIsCreatedForTheUsersId()
    {
        var user = Ada();

        var viewModel = new UserDetailViewModel(user, null, new FakeEmailFactory());

        Assert.Equal(user.Id, Assert.IsType<FakeAddressesViewModel>(viewModel.EmailAddresses).ObjectId);
        Assert.Null(viewModel.Addresses);
    }

    [Fact]
    public void PhoneNumbersTabIsCreatedForTheUsersId()
    {
        var user = Ada();

        var viewModel = new UserDetailViewModel(user, null, null, new FakePhoneFactory());

        Assert.Equal(user.Id, Assert.IsType<FakeAddressesViewModel>(viewModel.PhoneNumbers).ObjectId);
    }

    [Fact]
    public void AddressesTabIsAbsentWhenTheAddressesModuleIsNotInstalled()
    {
        var viewModel = new UserDetailViewModel(Ada());

        Assert.Null(viewModel.Addresses);
        Assert.Null(viewModel.EmailAddresses);
        Assert.Null(viewModel.PhoneNumbers);
    }
}

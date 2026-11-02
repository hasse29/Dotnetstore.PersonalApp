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

        var addresses = viewModel.Addresses.ShouldBeOfType<FakeAddressesViewModel>();
        addresses.ObjectId.ShouldBe(user.Id);
    }

    [Fact]
    public void EmailAddressesTabIsCreatedForTheUsersId()
    {
        var user = Ada();

        var viewModel = new UserDetailViewModel(user, null, new FakeEmailFactory());

        (viewModel.EmailAddresses.ShouldBeOfType<FakeAddressesViewModel>().ObjectId).ShouldBe(user.Id);
        viewModel.Addresses.ShouldBeNull();
    }

    [Fact]
    public void PhoneNumbersTabIsCreatedForTheUsersId()
    {
        var user = Ada();

        var viewModel = new UserDetailViewModel(user, null, null, new FakePhoneFactory());

        (viewModel.PhoneNumbers.ShouldBeOfType<FakeAddressesViewModel>().ObjectId).ShouldBe(user.Id);
    }

    [Fact]
    public void AddressesTabIsAbsentWhenTheAddressesModuleIsNotInstalled()
    {
        var viewModel = new UserDetailViewModel(Ada());

        viewModel.Addresses.ShouldBeNull();
        viewModel.EmailAddresses.ShouldBeNull();
        viewModel.PhoneNumbers.ShouldBeNull();
    }
}

using Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;
using Dotnetstore.PersonalApp.Shared.Addresses;
using Dotnetstore.PersonalApp.Shared.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.TimeReports;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ClientDetailViewModelTests
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

    private sealed class FakeTimeReportsFactory : ITimeReportsPanelFactory
    {
        public ViewModelBase CreateForClient(Guid clientId) => new FakeAddressesViewModel(clientId);
    }

    [Fact]
    public void TimeReportsTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, null, null, null, new FakeTimeReportsFactory());

        (viewModel.TimeReports.ShouldBeOfType<FakeAddressesViewModel>().ObjectId).ShouldBe(client.Id);
    }

    private static ClientDto Acme() =>
        new(Guid.NewGuid(), "Acme Tools AB", "556677-8899", null, null, null, null, null, DateTimeOffset.UnixEpoch);

    [Fact]
    public void InitialsComeFromTheFirstTwoWordsOfTheName()
    {
        (new ClientDetailViewModel(Acme()).Initials).ShouldBe("AT");
    }

    [Fact]
    public void AddressesTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, new FakeFactory());

        (viewModel.Addresses.ShouldBeOfType<FakeAddressesViewModel>().ObjectId).ShouldBe(client.Id);
    }

    [Fact]
    public void EmailAddressesTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, null, new FakeEmailFactory());

        (viewModel.EmailAddresses.ShouldBeOfType<FakeAddressesViewModel>().ObjectId).ShouldBe(client.Id);
    }

    [Fact]
    public void PhoneNumbersTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, null, null, new FakePhoneFactory());

        (viewModel.PhoneNumbers.ShouldBeOfType<FakeAddressesViewModel>().ObjectId).ShouldBe(client.Id);
    }

    [Fact]
    public void TabsAreAbsentWhenTheirModulesAreNotInstalled()
    {
        var viewModel = new ClientDetailViewModel(Acme());

        viewModel.Addresses.ShouldBeNull();
        viewModel.EmailAddresses.ShouldBeNull();
        viewModel.PhoneNumbers.ShouldBeNull();
    }

    [Fact]
    public void BackAndEditRaiseTheirEvents()
    {
        var viewModel = new ClientDetailViewModel(Acme());
        var closed = 0;
        var editRequested = 0;
        viewModel.Closed += (_, _) => closed++;
        viewModel.EditRequested += (_, _) => editRequested++;

        viewModel.BackCommand.Execute(null);
        viewModel.EditCommand.Execute(null);

        closed.ShouldBe(1);
        editRequested.ShouldBe(1);
    }
}

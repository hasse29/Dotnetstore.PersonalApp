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

        Assert.Equal(client.Id, Assert.IsType<FakeAddressesViewModel>(viewModel.TimeReports).ObjectId);
    }

    private static ClientDto Acme() =>
        new(Guid.NewGuid(), "Acme Tools AB", "556677-8899", null, null, null, null, null, DateTimeOffset.UnixEpoch);

    [Fact]
    public void InitialsComeFromTheFirstTwoWordsOfTheName()
    {
        Assert.Equal("AT", new ClientDetailViewModel(Acme()).Initials);
    }

    [Fact]
    public void AddressesTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, new FakeFactory());

        Assert.Equal(client.Id, Assert.IsType<FakeAddressesViewModel>(viewModel.Addresses).ObjectId);
    }

    [Fact]
    public void EmailAddressesTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, null, new FakeEmailFactory());

        Assert.Equal(client.Id, Assert.IsType<FakeAddressesViewModel>(viewModel.EmailAddresses).ObjectId);
    }

    [Fact]
    public void PhoneNumbersTabIsCreatedForTheClientsId()
    {
        var client = Acme();

        var viewModel = new ClientDetailViewModel(client, null, null, new FakePhoneFactory());

        Assert.Equal(client.Id, Assert.IsType<FakeAddressesViewModel>(viewModel.PhoneNumbers).ObjectId);
    }

    [Fact]
    public void TabsAreAbsentWhenTheirModulesAreNotInstalled()
    {
        var viewModel = new ClientDetailViewModel(Acme());

        Assert.Null(viewModel.Addresses);
        Assert.Null(viewModel.EmailAddresses);
        Assert.Null(viewModel.PhoneNumbers);
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

        Assert.Equal(1, closed);
        Assert.Equal(1, editRequested);
    }
}

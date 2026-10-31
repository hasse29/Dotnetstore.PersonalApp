using Dotnetstore.PersonalApp.Modules.Addresses.Consumers;
using Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Tests;

public sealed class OwnerDeletedConsumerTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UserDeletedRemovesTheUsersAddressesOnly()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(user), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(user, city: "Lund"), Ct);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(other), Ct);

        await new UserDeletedConsumer(_fixture.DeleteAddressesForObject).ConsumeAsync(new UserDeleted(user), Ct);

        Assert.Empty((await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(user), Ct)).Items);
        Assert.Single((await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(other), Ct)).Items);
    }

    [Fact]
    public async Task ClientDeletedRemovesTheClientsAddresses()
    {
        var client = Guid.NewGuid();
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(client), Ct);

        await new ClientDeletedConsumer(_fixture.DeleteAddressesForObject).ConsumeAsync(new ClientDeleted(client), Ct);

        Assert.Empty((await _fixture.ListAddresses.HandleAsync(new ListAddresses.Query(client), Ct)).Items);
    }
}

using Dotnetstore.PersonalApp.Shared.Clients;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ClientLookupTests : IDisposable
{
    private readonly ClientsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ExistsIsTrueOnlyForSavedClients()
    {
        IClientLookup lookup = new ClientLookup(_fixture.DbFactory);
        var clientId = (await _fixture.Create.HandleAsync(ClientsFixture.ValidCommand(), Ct)).Value.Id;

        (await lookup.ExistsAsync(clientId, Ct)).ShouldBeTrue();
        (await lookup.ExistsAsync(Guid.NewGuid(), Ct)).ShouldBeFalse();
    }
}

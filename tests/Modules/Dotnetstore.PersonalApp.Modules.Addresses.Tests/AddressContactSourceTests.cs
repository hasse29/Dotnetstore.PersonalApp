using Dotnetstore.PersonalApp.Shared.Contacts;
using Xunit;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Tests;

public sealed class AddressContactSourceTests : IDisposable
{
    private readonly AddressesFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ListsTheObjectsAddressesWithTypeNameAndOneLineValue()
    {
        var owner = Guid.NewGuid();
        var created = await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(owner), TestContext.Current.CancellationToken);
        await _fixture.CreateAddress.HandleAsync(_fixture.ValidAddress(Guid.NewGuid()), TestContext.Current.CancellationToken);

        var entries = await new AddressContactSource(_fixture.DbFactory).ListAsync(owner, TestContext.Current.CancellationToken);

        var entry = entries.ShouldHaveSingleItem();
        entry.Id.ShouldBe(created.Value.Id);
        entry.Kind.ShouldBe(ContactKind.Address);
        entry.TypeName.ShouldBe("Billing");
        entry.Value.ShouldBe("Ada Lovelace, Drottninggatan 1, Floor 3, 111 51 Stockholm, Sweden");
    }
}

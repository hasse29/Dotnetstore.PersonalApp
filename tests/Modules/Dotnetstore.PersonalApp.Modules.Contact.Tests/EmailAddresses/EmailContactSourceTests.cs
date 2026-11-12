using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.EmailAddresses;

public sealed class EmailContactSourceTests : IDisposable
{
    private readonly EmailAddressesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ListsOnlyTheObjectsEmailAddressesOrderedByTypeThenAddress()
    {
        var owner = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(owner, _fixture.WorkTypeId, "z@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(owner, _fixture.BillingTypeId, "b@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(owner, _fixture.WorkTypeId, "a@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(Guid.NewGuid()), Ct);

        var source = new EmailContactSource(_fixture.DbFactory);
        var entries = await source.ListAsync(owner, Ct);

        source.Kind.ShouldBe(ContactKind.Email);
        entries.Select(e => e.Value).ShouldBe(["b@example.com", "a@example.com", "z@example.com"]);
        entries.ShouldAllBe(e => e.Kind == ContactKind.Email);
        entries[0].TypeName.ShouldBe("Billing");
    }

    [Fact]
    public async Task AnObjectWithoutEmailAddressesHasNoEntries() =>
        (await new EmailContactSource(_fixture.DbFactory).ListAsync(Guid.NewGuid(), Ct)).ShouldBeEmpty();
}

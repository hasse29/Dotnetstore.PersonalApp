using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Contacts;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.PhoneNumbers;

public sealed class PhoneContactSourceTests : IDisposable
{
    private readonly PhoneNumbersFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ListsOnlyTheObjectsPhoneNumbersOrderedByTypeThenNumber()
    {
        var owner = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(owner, _fixture.WorkTypeId, "+46 8 999"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(owner, _fixture.MobileTypeId, "+46 70 222"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(owner, _fixture.WorkTypeId, "+46 8 111"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(Guid.NewGuid()), Ct);

        var source = new PhoneContactSource(_fixture.DbFactory);
        var entries = await source.ListAsync(owner, Ct);

        source.Kind.ShouldBe(ContactKind.Phone);
        entries.Select(e => e.Value).ShouldBe(["+46 70 222", "+46 8 111", "+46 8 999"]);
        entries.ShouldAllBe(e => e.Kind == ContactKind.Phone);
        entries[0].TypeName.ShouldBe("Mobile");
    }

    [Fact]
    public async Task AnObjectWithoutPhoneNumbersHasNoEntries() =>
        (await new PhoneContactSource(_fixture.DbFactory).ListAsync(Guid.NewGuid(), Ct)).ShouldBeEmpty();
}

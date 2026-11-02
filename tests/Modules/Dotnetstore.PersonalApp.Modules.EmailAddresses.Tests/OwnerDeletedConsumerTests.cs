using Dotnetstore.PersonalApp.Modules.EmailAddresses.Consumers;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Tests;

public sealed class OwnerDeletedConsumerTests : IDisposable
{
    private readonly EmailAddressesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UserDeletedRemovesTheUsersEmailAddressesOnly()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(user), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(user, email: "second@example.com"), Ct);
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(other), Ct);

        await new UserDeletedConsumer(_fixture.DeleteEmailAddressesForObject).ConsumeAsync(new UserDeleted(user), Ct);

        (await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(user), Ct)).Items.ShouldBeEmpty();
        (await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(other), Ct)).Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ClientDeletedRemovesTheClientsEmailAddresses()
    {
        var client = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(client), Ct);

        await new ClientDeletedConsumer(_fixture.DeleteEmailAddressesForObject).ConsumeAsync(new ClientDeleted(client), Ct);

        (await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(client), Ct)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ContactPersonDeletedRemovesTheContactPersonsEntries()
    {
        var person = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(person), Ct);

        await new ContactPersonDeletedConsumer(_fixture.DeleteEmailAddressesForObject).ConsumeAsync(new ContactPersonDeleted(person), Ct);

        (await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(person), Ct)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task StudentDeletedRemovesTheStudentsEntries()
    {
        var student = Guid.NewGuid();
        await _fixture.CreateEmailAddress.HandleAsync(_fixture.ValidEmailAddress(student), Ct);

        await new StudentDeletedConsumer(_fixture.DeleteEmailAddressesForObject).ConsumeAsync(new StudentDeleted(student), Ct);

        (await _fixture.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(student), Ct)).Items.ShouldBeEmpty();
    }
}

using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Consumers;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Tests;

public sealed class OwnerDeletedConsumerTests : IDisposable
{
    private readonly PhoneNumbersFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UserDeletedRemovesTheUsersPhoneNumbersOnly()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(user), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(user, number: "+46 70 999 999"), Ct);
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(other), Ct);

        await new UserDeletedConsumer(_fixture.DeletePhoneNumbersForObject).ConsumeAsync(new UserDeleted(user), Ct);

        Assert.Empty((await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(user), Ct)).Items);
        Assert.Single((await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(other), Ct)).Items);
    }

    [Fact]
    public async Task ClientDeletedRemovesTheClientsPhoneNumbers()
    {
        var client = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(client), Ct);

        await new ClientDeletedConsumer(_fixture.DeletePhoneNumbersForObject).ConsumeAsync(new ClientDeleted(client), Ct);

        Assert.Empty((await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(client), Ct)).Items);
    }

    [Fact]
    public async Task ContactPersonDeletedRemovesTheContactPersonsEntries()
    {
        var person = Guid.NewGuid();
        await _fixture.CreatePhoneNumber.HandleAsync(_fixture.ValidPhoneNumber(person), Ct);

        await new ContactPersonDeletedConsumer(_fixture.DeletePhoneNumbersForObject).ConsumeAsync(new ContactPersonDeleted(person), Ct);

        Assert.Empty((await _fixture.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(person), Ct)).Items);
    }
}

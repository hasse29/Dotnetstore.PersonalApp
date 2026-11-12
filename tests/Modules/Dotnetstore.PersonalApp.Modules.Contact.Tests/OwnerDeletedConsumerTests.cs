using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Consumers;
using Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Features.PhoneNumbers;
using Dotnetstore.PersonalApp.Modules.Contact.Tests.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Tests.EmailAddresses;
using Dotnetstore.PersonalApp.Modules.Contact.Tests.PhoneNumbers;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests;

public sealed class OwnerDeletedConsumerTests : IDisposable
{
    private readonly AddressesFixture _addresses = new();
    private readonly EmailAddressesFixture _emails = new();
    private readonly PhoneNumbersFixture _phones = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private OwnerDeletedConsumer Consumer => new(_addresses.DeleteAddressesForObject, _emails.DeleteEmailAddressesForObject, _phones.DeletePhoneNumbersForObject);

    public void Dispose()
    {
        _addresses.Dispose();
        _emails.Dispose();
        _phones.Dispose();
    }

    private async Task AddEverythingAsync(Guid owner)
    {
        await _addresses.CreateAddress.HandleAsync(_addresses.ValidAddress(owner), Ct);
        await _emails.CreateEmailAddress.HandleAsync(_emails.ValidEmailAddress(owner), Ct);
        await _phones.CreatePhoneNumber.HandleAsync(_phones.ValidPhoneNumber(owner), Ct);
    }

    private async Task<int> CountAsync(Guid owner) =>
        (await _addresses.ListAddresses.HandleAsync(new ListAddresses.Query(owner), Ct)).Items.Count
        + (await _emails.ListEmailAddresses.HandleAsync(new ListEmailAddresses.Query(owner), Ct)).Items.Count
        + (await _phones.ListPhoneNumbers.HandleAsync(new ListPhoneNumbers.Query(owner), Ct)).Items.Count;

    [Fact]
    public async Task ADeletedOwnerLosesAllThreeKindsOfContactDetailsAndNobodyElseDoes()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        await AddEverythingAsync(owner);
        await AddEverythingAsync(other);

        await Consumer.ConsumeAsync(new UserDeleted(owner), Ct);

        (await CountAsync(owner)).ShouldBe(0);
        (await CountAsync(other)).ShouldBe(3);
    }

    [Fact]
    public async Task EveryKindOfOwnerThatCanBeDeletedIsHandled()
    {
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var id in ids)
        {
            await AddEverythingAsync(id);
        }

        await ((IIntegrationEventConsumer<ClientDeleted>)Consumer).ConsumeAsync(new ClientDeleted(ids[0]), Ct);
        await ((IIntegrationEventConsumer<ContactPersonDeleted>)Consumer).ConsumeAsync(new ContactPersonDeleted(ids[1]), Ct);
        await ((IIntegrationEventConsumer<CustomerDeleted>)Consumer).ConsumeAsync(new CustomerDeleted(ids[2]), Ct);
        await ((IIntegrationEventConsumer<StudentDeleted>)Consumer).ConsumeAsync(new StudentDeleted(ids[3]), Ct);

        foreach (var id in ids)
        {
            (await CountAsync(id)).ShouldBe(0);
        }
    }
}

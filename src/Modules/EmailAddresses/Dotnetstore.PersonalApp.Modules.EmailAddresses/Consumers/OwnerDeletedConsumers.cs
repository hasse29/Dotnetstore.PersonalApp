using Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Consumers;

/// <summary>A deleted user's addresses are of no use to anyone, so they go with the user.</summary>
internal sealed class UserDeletedConsumer(DeleteEmailAddressesForObject.Handler deleteAll) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteEmailAddressesForObject.Command(integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

internal sealed class ClientDeletedConsumer(DeleteEmailAddressesForObject.Handler deleteAll) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteEmailAddressesForObject.Command(integrationEvent.ClientId), cancellationToken).ConfigureAwait(false);
}

internal sealed class ContactPersonDeletedConsumer(DeleteEmailAddressesForObject.Handler deleteAll) : IIntegrationEventConsumer<ContactPersonDeleted>
{
    public async Task ConsumeAsync(ContactPersonDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteEmailAddressesForObject.Command(integrationEvent.ContactPersonId), cancellationToken).ConfigureAwait(false);
}

using Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Consumers;

/// <summary>A deleted user's addresses are of no use to anyone, so they go with the user.</summary>
internal sealed class UserDeletedConsumer(DeleteAddressesForObject.Handler deleteAll) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteAddressesForObject.Command(integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

internal sealed class ClientDeletedConsumer(DeleteAddressesForObject.Handler deleteAll) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteAddressesForObject.Command(integrationEvent.ClientId), cancellationToken).ConfigureAwait(false);
}

using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Consumers;

/// <summary>Nobody teaches at a school that no longer exists.</summary>
internal sealed class ClientDeletedConsumer(DeleteScheduleEntriesFor.Handler deleteFor) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteFor.HandleAsync(new DeleteScheduleEntriesFor.Command(ClientId: integrationEvent.ClientId), cancellationToken).ConfigureAwait(false);
}

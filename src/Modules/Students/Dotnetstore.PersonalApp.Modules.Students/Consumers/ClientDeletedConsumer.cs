using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Students.Consumers;

/// <summary>Students belong to a school, so they go with it.</summary>
internal sealed class ClientDeletedConsumer(DeleteStudentsForClient.Handler deleteAll) : IIntegrationEventConsumer<ClientDeleted>
{
    public async Task ConsumeAsync(ClientDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteStudentsForClient.Command(integrationEvent.ClientId), cancellationToken).ConfigureAwait(false);
}

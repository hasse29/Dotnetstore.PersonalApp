using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Consumers;

/// <summary>A deleted teacher no longer has a schedule.</summary>
internal sealed class UserDeletedConsumer(DeleteScheduleEntriesFor.Handler deleteFor) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteFor.HandleAsync(new DeleteScheduleEntriesFor.Command(UserId: integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

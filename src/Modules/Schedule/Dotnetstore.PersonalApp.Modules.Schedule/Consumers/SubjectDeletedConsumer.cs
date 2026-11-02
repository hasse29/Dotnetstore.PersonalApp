using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Consumers;

/// <summary>A lesson in a subject that was deleted from the catalogue is removed with it.</summary>
internal sealed class SubjectDeletedConsumer(DeleteScheduleEntriesFor.Handler deleteFor) : IIntegrationEventConsumer<SubjectDeleted>
{
    public async Task ConsumeAsync(SubjectDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteFor.HandleAsync(new DeleteScheduleEntriesFor.Command(SubjectId: integrationEvent.SubjectId), cancellationToken).ConfigureAwait(false);
}

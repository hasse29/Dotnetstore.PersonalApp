using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Consumers;

/// <summary>A deleted student no longer attends any lesson.</summary>
internal sealed class StudentDeletedConsumer(RemoveStudentFromEntries.Handler remove) : IIntegrationEventConsumer<StudentDeleted>
{
    public async Task ConsumeAsync(StudentDeleted integrationEvent, CancellationToken cancellationToken) =>
        await remove.HandleAsync(new RemoveStudentFromEntries.Command(integrationEvent.StudentId), cancellationToken).ConfigureAwait(false);
}

using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Grades.Consumers;

/// <summary>Grades in a deleted subject go with it.</summary>
internal sealed class SubjectDeletedConsumer(DeleteGradesFor.Handler deleteFor) : IIntegrationEventConsumer<SubjectDeleted>
{
    public async Task ConsumeAsync(SubjectDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteFor.HandleAsync(new DeleteGradesFor.Command(SubjectId: integrationEvent.SubjectId), cancellationToken).ConfigureAwait(false);
}

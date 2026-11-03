using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Grades.Consumers;

/// <summary>A deleted student's grades go with them.</summary>
internal sealed class StudentDeletedConsumer(DeleteGradesFor.Handler deleteFor) : IIntegrationEventConsumer<StudentDeleted>
{
    public async Task ConsumeAsync(StudentDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteFor.HandleAsync(new DeleteGradesFor.Command(StudentId: integrationEvent.StudentId), cancellationToken).ConfigureAwait(false);
}

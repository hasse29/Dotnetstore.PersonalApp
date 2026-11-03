using Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Grades.Consumers;

/// <summary>The grades a deleted teacher gave go with them.</summary>
internal sealed class UserDeletedConsumer(DeleteGradesFor.Handler deleteFor) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteFor.HandleAsync(new DeleteGradesFor.Command(UserId: integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

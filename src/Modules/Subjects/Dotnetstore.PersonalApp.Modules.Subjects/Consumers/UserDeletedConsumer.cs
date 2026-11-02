using Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Consumers;

/// <summary>A deleted teacher no longer teaches anything.</summary>
internal sealed class UserDeletedConsumer(DeleteTeacherSubjectsForUser.Handler deleteAll) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteTeacherSubjectsForUser.Command(integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

using Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Consumers;

/// <summary>A CV is personal data, so a deleted user's CVs go with the user.</summary>
internal sealed class UserDeletedConsumer(DeleteCvsForUser.Handler deleteAll) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteCvsForUser.Command(integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Email.Consumers;

/// <summary>Mail credentials are personal data, so a deleted user's own accounts go with the user. System accounts stay.</summary>
internal sealed class UserDeletedConsumer(DeleteEmailAccountsForUser.Handler deleteAll) : IIntegrationEventConsumer<UserDeleted>
{
    public async Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken) =>
        await deleteAll.HandleAsync(new DeleteEmailAccountsForUser.Command(integrationEvent.UserId), cancellationToken).ConfigureAwait(false);
}

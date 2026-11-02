using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Host.Events;

/// <summary>Lets the open shell hear that a user's photo changed, so the top bar and sidebar follow without a restart.</summary>
internal sealed class UserPhotoNotifier : IIntegrationEventConsumer<UserPhotoChanged>
{
    /// <summary>Raised with the id of the user whose photo changed, on whatever thread the publisher is on.</summary>
    public event EventHandler<Guid>? Changed;

    public Task ConsumeAsync(UserPhotoChanged integrationEvent, CancellationToken cancellationToken)
    {
        Changed?.Invoke(this, integrationEvent.UserId);
        return Task.CompletedTask;
    }
}

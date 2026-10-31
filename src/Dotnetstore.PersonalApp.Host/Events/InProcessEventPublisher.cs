using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Host.Events;

/// <summary>
/// Delivers integration events to the handlers modules registered in DI. Handlers run one after another in the
/// publisher's context; an exception from a handler propagates to the publisher.
/// </summary>
internal sealed class InProcessEventPublisher(IServiceProvider services) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : notnull
    {
        foreach (var handler in services.GetServices<IIntegrationEventConsumer<TEvent>>())
        {
            await handler.ConsumeAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}

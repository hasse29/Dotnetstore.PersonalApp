namespace Dotnetstore.PersonalApp.Shared.Events;

public interface IEventPublisher
{
    /// <summary>Delivers the event, in process, to every registered <see cref="IIntegrationEventConsumer{TEvent}"/>.</summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : notnull;
}

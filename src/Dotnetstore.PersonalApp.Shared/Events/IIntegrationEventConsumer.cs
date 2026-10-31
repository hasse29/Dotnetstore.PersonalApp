namespace Dotnetstore.PersonalApp.Shared.Events;

public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : notnull
{
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}

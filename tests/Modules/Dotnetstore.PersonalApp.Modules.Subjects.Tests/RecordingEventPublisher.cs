using Dotnetstore.PersonalApp.Shared.Events;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

/// <summary>Remembers the integration events a handler publishes.</summary>
internal sealed class RecordingEventPublisher : IEventPublisher
{
    public List<object> Published { get; } = [];

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : notnull
    {
        Published.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

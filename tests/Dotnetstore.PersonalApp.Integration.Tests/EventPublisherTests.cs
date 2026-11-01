using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.Events;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class EventPublisherTests
{
    private sealed class Recorder(List<string> log, string name) : IIntegrationEventConsumer<UserDeleted>
    {
        public Task ConsumeAsync(UserDeleted integrationEvent, CancellationToken cancellationToken)
        {
            log.Add(name + ":" + integrationEvent.UserId);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DeliversTheEventToEveryHandlerInRegistrationOrder()
    {
        var log = new List<string>();
        var userId = Guid.NewGuid();
        await using var provider = new ServiceCollection()
            .AddSingleton<IIntegrationEventConsumer<UserDeleted>>(new Recorder(log, "first"))
            .AddSingleton<IIntegrationEventConsumer<UserDeleted>>(new Recorder(log, "second"))
            .BuildServiceProvider();

        await new InProcessEventPublisher(provider).PublishAsync(new UserDeleted(userId), TestContext.Current.CancellationToken);

        Assert.Equal(["first:" + userId, "second:" + userId], log);
    }

    [Fact]
    public async Task PublishingWithoutHandlersIsANoOp()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();

        await new InProcessEventPublisher(provider).PublishAsync(new ClientDeleted(Guid.NewGuid()), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void TheOwnedDataModulesListenForUserAndClientDeletions()
    {
        using var services = CompositionRoot.BuildServices();

        Assert.Equal(5, services.GetServices<IIntegrationEventConsumer<UserDeleted>>().Count());
        Assert.Equal(3, services.GetServices<IIntegrationEventConsumer<ClientDeleted>>().Count());
        Assert.IsType<InProcessEventPublisher>(services.GetRequiredService<IEventPublisher>());
    }
}

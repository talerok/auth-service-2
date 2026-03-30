using Auth.Application;
using Auth.Application.Messaging;

namespace Auth.IntegrationTests.Stubs;

internal sealed class StubEventBus : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, IIntegrationEvent
        => Task.CompletedTask;
}

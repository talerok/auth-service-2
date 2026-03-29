namespace Auth.Application;

public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class, Messaging.IIntegrationEvent;
}

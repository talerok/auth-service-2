using Auth.Application;

namespace Auth.Infrastructure.Integration.Kafka;

public sealed class NullKafkaProducer : IKafkaProducer
{
    public Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken) => Task.CompletedTask;
}

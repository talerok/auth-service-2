namespace Auth.Application;

public interface IKafkaProducer
{
    Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken);
}

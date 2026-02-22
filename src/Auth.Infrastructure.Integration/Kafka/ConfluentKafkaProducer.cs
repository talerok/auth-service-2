using Auth.Application;
using Auth.Infrastructure;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Integration.Kafka;

public sealed class ConfluentKafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public ConfluentKafkaProducer(IOptions<IntegrationOptions> options)
    {
        var kafka = options.Value.Kafka;
        var config = new ProducerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            EnableIdempotence = kafka.Producer.EnableIdempotence,
            Acks = ParseAcks(kafka.Producer.Acks),
            MessageSendMaxRetries = int.MaxValue
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken)
    {
        var message = new Message<string, string> { Key = key, Value = payload };
        await _producer.ProduceAsync(topic, message, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private static Acks ParseAcks(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "none" => Acks.None,
            "leader" => Acks.Leader,
            _ => Acks.All
        };
    }
}

namespace Auth.Application.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    string? CorrelationId { get; }
}

public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

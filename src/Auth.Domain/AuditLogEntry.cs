namespace Auth.Domain;

public sealed class AuditLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid? ActorId { get; init; }
    public string? ActorName { get; init; }
    public AuditActorType ActorType { get; init; }
    public AuditEntityType EntityType { get; init; }
    public Guid EntityId { get; init; }
    public AuditAction Action { get; init; }
    public Dictionary<string, object?>? Details { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? CorrelationId { get; init; }
}

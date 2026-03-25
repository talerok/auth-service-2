using Auth.Domain;

namespace Auth.Application;

public sealed record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    Guid? ActorId,
    string? ActorName,
    AuditActorType ActorType,
    AuditEntityType EntityType,
    Guid EntityId,
    AuditAction Action,
    Dictionary<string, object?>? Details,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);

using Auth.Domain;

namespace Auth.Application;

/// <summary>
/// Marker for commands that should be automatically logged via AuditBehavior.
/// </summary>
public interface IAuditable
{
    AuditEntityType EntityType { get; }
    AuditAction Action { get; }
    Guid EntityId { get; }
    bool Critical => false;
}

public sealed record AuditActor(Guid Id, string Name, AuditActorType Type = AuditActorType.User);

public interface IAuditActorProvider
{
    AuditActor? GetAuditActor();
}

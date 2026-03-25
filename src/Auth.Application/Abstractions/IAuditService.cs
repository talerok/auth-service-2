using Auth.Domain;

namespace Auth.Application;

public interface IAuditService
{
    Task LogAsync(
        AuditEntityType entityType,
        Guid entityId,
        AuditAction action,
        Dictionary<string, object?>? details = null,
        AuditActor? actor = null,
        bool critical = false,
        CancellationToken cancellationToken = default);
}

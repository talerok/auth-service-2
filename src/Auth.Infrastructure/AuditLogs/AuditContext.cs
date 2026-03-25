using Auth.Application;

namespace Auth.Infrastructure.AuditLogs;

internal sealed class AuditContext : IAuditContext
{
    public Dictionary<string, object?>? Details { get; set; }
    public Guid? EntityId { get; set; }
}

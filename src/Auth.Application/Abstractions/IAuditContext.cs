namespace Auth.Application;

/// <summary>
/// Scoped service for passing data from handler to AuditBehavior.
/// Handler writes — behavior reads.
/// </summary>
public interface IAuditContext
{
    Dictionary<string, object?>? Details { get; set; }
    Guid? EntityId { get; set; }
}

namespace Auth.Application.Messaging.Commands;

public sealed record IndexEntityRequested : IntegrationEventBase
{
    public required IndexEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required IndexOperation Operation { get; init; }
}

public enum IndexEntityType
{
    User,
    Role,
    Permission,
    Workspace,
    Application,
    ServiceAccount,
    NotificationTemplate,
    Session
}

public enum IndexOperation
{
    Index,
    Delete
}

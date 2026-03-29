namespace Auth.Application.Messaging.Events;

public sealed record WorkspaceCreatedEvent : IntegrationEventBase
{
    public required Guid WorkspaceId { get; init; }
    public required string Code { get; init; }
}

public sealed record WorkspaceUpdatedEvent : IntegrationEventBase
{
    public required Guid WorkspaceId { get; init; }
}

public sealed record WorkspaceDeletedEvent : IntegrationEventBase
{
    public required Guid WorkspaceId { get; init; }
}

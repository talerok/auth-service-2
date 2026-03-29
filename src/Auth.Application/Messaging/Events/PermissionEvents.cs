namespace Auth.Application.Messaging.Events;

public sealed record PermissionCreatedEvent : IntegrationEventBase
{
    public required Guid PermissionId { get; init; }
    public required string Code { get; init; }
    public required string Domain { get; init; }
    public required int Bit { get; init; }
}

public sealed record PermissionUpdatedEvent : IntegrationEventBase
{
    public required Guid PermissionId { get; init; }
    public required string Code { get; init; }
}

public sealed record PermissionDeletedEvent : IntegrationEventBase
{
    public required Guid PermissionId { get; init; }
    public required string Code { get; init; }
}

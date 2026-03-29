namespace Auth.Application.Messaging.Events;

public sealed record RoleCreatedEvent : IntegrationEventBase
{
    public required Guid RoleId { get; init; }
    public required string Name { get; init; }
}

public sealed record RoleUpdatedEvent : IntegrationEventBase
{
    public required Guid RoleId { get; init; }
    public required string[] ChangedFields { get; init; }
}

public sealed record RoleDeletedEvent : IntegrationEventBase
{
    public required Guid RoleId { get; init; }
}

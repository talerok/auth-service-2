namespace Auth.Application.Messaging.Events;

public sealed record UserCreatedEvent : IntegrationEventBase
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
}

public sealed record UserUpdatedEvent : IntegrationEventBase
{
    public required Guid UserId { get; init; }
    public required string[] ChangedFields { get; init; }
}

public sealed record UserDeletedEvent : IntegrationEventBase
{
    public required Guid UserId { get; init; }
}

public sealed record UserBlockedEvent : IntegrationEventBase
{
    public required Guid UserId { get; init; }
}

public sealed record UserAuthenticatedEvent : IntegrationEventBase
{
    public required Guid UserId { get; init; }
    public required string AuthMethod { get; init; }
    public string? IpAddress { get; init; }
    public string? ClientId { get; init; }
}

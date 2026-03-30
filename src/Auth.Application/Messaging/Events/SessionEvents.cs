namespace Auth.Application.Messaging.Events;

public sealed record SessionRevokedEvent : IntegrationEventBase
{
    public required Guid SessionId { get; init; }
    public required Guid UserId { get; init; }
    public required string Reason { get; init; }
}

namespace Auth.Application.Messaging.Commands;

public sealed record DeliverOtpRequested : IntegrationEventBase
{
    public required Guid ChallengeId { get; init; }
}

namespace Auth.Application.Messaging.Commands;

public sealed record IndexAuditLogRequested : IntegrationEventBase
{
    public required Guid AuditLogEntryId { get; init; }
}

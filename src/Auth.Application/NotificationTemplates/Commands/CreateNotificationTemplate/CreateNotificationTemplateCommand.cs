using Auth.Domain;
using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.CreateNotificationTemplate;

public sealed record CreateNotificationTemplateCommand(
    string Type,
    string Locale,
    string Subject,
    string Body) : IRequest<NotificationTemplateDto>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.NotificationTemplate;
    public AuditAction Action => AuditAction.Create;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}

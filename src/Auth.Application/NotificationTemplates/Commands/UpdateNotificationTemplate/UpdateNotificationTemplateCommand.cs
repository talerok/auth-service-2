using Auth.Domain;
using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;

public sealed record UpdateNotificationTemplateCommand(
    Guid Id,
    string Type,
    string Locale,
    string Subject,
    string Body) : IRequest<NotificationTemplateDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.NotificationTemplate;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}

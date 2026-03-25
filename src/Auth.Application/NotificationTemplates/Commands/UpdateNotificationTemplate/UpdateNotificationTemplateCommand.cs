using Auth.Domain;
using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.UpdateNotificationTemplate;

public sealed record UpdateNotificationTemplateCommand(string Channel, string Subject, string Body) : IRequest<NotificationTemplateDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.NotificationTemplate;
    public AuditAction Action => AuditAction.Update;
    // Resolved at runtime by handler via IAuditContext.EntityId (looked up by Channel)
    public Guid EntityId => Guid.Empty;
}

using Auth.Domain;
using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.SoftDeleteNotificationTemplate;

public sealed record SoftDeleteNotificationTemplateCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.NotificationTemplate;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}

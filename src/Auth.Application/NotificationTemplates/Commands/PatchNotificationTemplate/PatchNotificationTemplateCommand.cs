using Auth.Domain;
using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;

public sealed record PatchNotificationTemplateCommand(
    Guid Id,
    string? Type,
    string? Locale,
    string? Subject,
    string? Body) : IRequest<NotificationTemplateDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.NotificationTemplate;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}

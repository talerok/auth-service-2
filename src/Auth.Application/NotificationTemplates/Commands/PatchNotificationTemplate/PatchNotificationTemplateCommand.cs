using Auth.Application.Common;
using Auth.Domain;
using MediatR;

namespace Auth.Application.NotificationTemplates.Commands.PatchNotificationTemplate;

public sealed record PatchNotificationTemplateCommand(
    Guid Id,
    Optional<string> Type,
    Optional<string> Locale,
    Optional<string> Subject,
    Optional<string> Body) : IRequest<NotificationTemplateDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.NotificationTemplate;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}

using Auth.Domain;
using MediatR;

namespace Auth.Application.Permissions.Commands.SoftDeletePermission;

public sealed record SoftDeletePermissionCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Permission;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}

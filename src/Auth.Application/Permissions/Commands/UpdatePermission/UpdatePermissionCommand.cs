using Auth.Domain;
using MediatR;

namespace Auth.Application.Permissions.Commands.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid Id, string Code, string Description) : IRequest<PermissionDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Permission;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}

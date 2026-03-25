using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.SetRolePermissions;

public sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<PermissionDto> Permissions) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.SetPermissions;
    public Guid EntityId => RoleId;
}

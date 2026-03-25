using Auth.Domain;
using MediatR;

namespace Auth.Application.Permissions.Commands.PatchPermission;

public sealed record PatchPermissionCommand(Guid Id, string? Code, string? Description) : IRequest<PermissionDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Permission;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}

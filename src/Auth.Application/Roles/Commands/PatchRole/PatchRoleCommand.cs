using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.PatchRole;

public sealed record PatchRoleCommand(Guid Id, string? Name, string? Code, string? Description) : IRequest<RoleDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}

using Auth.Application.Common;
using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.PatchRole;

public sealed record PatchRoleCommand(Guid Id, Optional<string> Name, Optional<string> Code, Optional<string> Description) : IRequest<RoleDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}

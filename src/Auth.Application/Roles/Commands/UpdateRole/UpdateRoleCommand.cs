using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, string Code, string Description) : IRequest<RoleDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}

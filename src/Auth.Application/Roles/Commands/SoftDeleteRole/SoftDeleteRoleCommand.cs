using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.SoftDeleteRole;

public sealed record SoftDeleteRoleCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}

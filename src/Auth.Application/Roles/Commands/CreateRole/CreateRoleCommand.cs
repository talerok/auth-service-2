using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string Code, string Description) : IRequest<RoleDto>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.Create;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}

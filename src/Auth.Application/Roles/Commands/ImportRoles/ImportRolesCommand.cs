using Auth.Domain;
using MediatR;

namespace Auth.Application.Roles.Commands.ImportRoles;

public sealed record ImportRolesCommand(IReadOnlyCollection<ImportRoleItem> Items, bool Add = true, bool Edit = true) : IRequest<ImportRolesResult>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Role;
    public AuditAction Action => AuditAction.Import;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}

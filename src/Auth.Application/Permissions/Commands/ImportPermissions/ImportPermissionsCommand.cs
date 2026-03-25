using Auth.Domain;
using MediatR;

namespace Auth.Application.Permissions.Commands.ImportPermissions;

public sealed record ImportPermissionsCommand(IReadOnlyCollection<ImportPermissionItem> Items, bool Add = true, bool Edit = true) : IRequest<ImportPermissionsResult>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Permission;
    public AuditAction Action => AuditAction.Import;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}

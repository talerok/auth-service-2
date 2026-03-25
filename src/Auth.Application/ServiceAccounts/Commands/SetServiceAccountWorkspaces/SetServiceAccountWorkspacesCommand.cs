using Auth.Domain;
using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.SetServiceAccountWorkspaces;

public sealed record SetServiceAccountWorkspacesCommand(
    Guid ServiceAccountId,
    IReadOnlyCollection<ServiceAccountWorkspaceRolesItem> Workspaces) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.ServiceAccount;
    public AuditAction Action => AuditAction.SetWorkspaces;
    public Guid EntityId => ServiceAccountId;
}

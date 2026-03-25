using Auth.Domain;
using MediatR;

namespace Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;

public sealed record SoftDeleteWorkspaceCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Workspace;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}

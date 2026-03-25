using Auth.Domain;
using MediatR;

namespace Auth.Application.Workspaces.Commands.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(Guid Id, string Name, string Code, string Description) : IRequest<WorkspaceDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Workspace;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}

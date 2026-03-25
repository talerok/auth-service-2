using Auth.Domain;
using MediatR;

namespace Auth.Application.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(string Name, string Code, string Description, bool IsSystem = false) : IRequest<WorkspaceDto>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Workspace;
    public AuditAction Action => AuditAction.Create;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}

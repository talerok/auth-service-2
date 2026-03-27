using Auth.Application.Common;
using Auth.Domain;
using MediatR;

namespace Auth.Application.Workspaces.Commands.PatchWorkspace;

public sealed record PatchWorkspaceCommand(Guid Id, Optional<string> Name, Optional<string> Code, Optional<string> Description) : IRequest<WorkspaceDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Workspace;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}

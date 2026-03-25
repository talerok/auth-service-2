using Auth.Domain;
using MediatR;

namespace Auth.Application.Workspaces.Commands.ImportWorkspaces;

public sealed record ImportWorkspacesCommand(IReadOnlyCollection<ImportWorkspaceItem> Items, bool Add = true, bool Edit = true) : IRequest<ImportWorkspacesResult>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Workspace;
    public AuditAction Action => AuditAction.Import;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}

using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.SetUserWorkspaces;

public sealed record SetUserWorkspacesCommand(
    Guid UserId,
    IReadOnlyCollection<UserWorkspaceRolesItem> Workspaces) : IRequest, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.SetWorkspaces;
    public Guid EntityId => UserId;
}

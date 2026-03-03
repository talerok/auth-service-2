using MediatR;

namespace Auth.Application.Users.Commands.SetUserWorkspaces;

public sealed record SetUserWorkspacesCommand(
    Guid UserId,
    IReadOnlyCollection<UserWorkspaceRolesItem> Workspaces) : IRequest;

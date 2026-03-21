using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.SetServiceAccountWorkspaces;

public sealed record SetServiceAccountWorkspacesCommand(
    Guid ServiceAccountId,
    IReadOnlyCollection<ServiceAccountWorkspaceRolesItem> Workspaces) : IRequest;

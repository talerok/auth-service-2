using MediatR;

namespace Auth.Application.ApiClients.Commands.SetApiClientWorkspaces;

public sealed record SetApiClientWorkspacesCommand(
    Guid ApiClientId,
    IReadOnlyCollection<ApiClientWorkspaceRolesItem> Workspaces) : IRequest;

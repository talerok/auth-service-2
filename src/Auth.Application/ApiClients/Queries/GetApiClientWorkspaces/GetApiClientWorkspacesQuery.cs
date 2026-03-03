using MediatR;

namespace Auth.Application.ApiClients.Queries.GetApiClientWorkspaces;

public sealed record GetApiClientWorkspacesQuery(Guid ApiClientId) : IRequest<IReadOnlyCollection<ApiClientWorkspaceRolesItem>?>;

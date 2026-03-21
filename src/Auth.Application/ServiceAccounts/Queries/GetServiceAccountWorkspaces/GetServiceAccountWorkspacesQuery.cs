using MediatR;

namespace Auth.Application.ServiceAccounts.Queries.GetServiceAccountWorkspaces;

public sealed record GetServiceAccountWorkspacesQuery(Guid ServiceAccountId) : IRequest<IReadOnlyCollection<ServiceAccountWorkspaceRolesItem>?>;

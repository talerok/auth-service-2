using MediatR;

namespace Auth.Application.Workspaces.Queries.GetAllWorkspaces;

public sealed record GetAllWorkspacesQuery() : IRequest<IReadOnlyCollection<WorkspaceDto>>;

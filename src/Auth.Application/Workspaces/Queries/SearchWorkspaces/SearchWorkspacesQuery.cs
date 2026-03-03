using MediatR;

namespace Auth.Application.Workspaces.Queries.SearchWorkspaces;

public sealed record SearchWorkspacesQuery(SearchRequest Request) : IRequest<SearchResponse<WorkspaceDto>>;

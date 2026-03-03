using Auth.Application;
using Auth.Application.Workspaces.Queries.SearchWorkspaces;
using MediatR;

namespace Auth.Infrastructure.Workspaces.Queries.SearchWorkspaces;

internal sealed class SearchWorkspacesQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchWorkspacesQuery, SearchResponse<WorkspaceDto>>
{
    public Task<SearchResponse<WorkspaceDto>> Handle(SearchWorkspacesQuery query, CancellationToken cancellationToken) =>
        searchService.SearchWorkspacesAsync(query.Request, cancellationToken);
}

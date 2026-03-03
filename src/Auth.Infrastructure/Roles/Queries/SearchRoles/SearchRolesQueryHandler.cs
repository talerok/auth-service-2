using Auth.Application;
using Auth.Application.Roles.Queries.SearchRoles;
using MediatR;

namespace Auth.Infrastructure.Roles.Queries.SearchRoles;

internal sealed class SearchRolesQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchRolesQuery, SearchResponse<RoleDto>>
{
    public Task<SearchResponse<RoleDto>> Handle(SearchRolesQuery query, CancellationToken cancellationToken) =>
        searchService.SearchRolesAsync(query.Request, cancellationToken);
}

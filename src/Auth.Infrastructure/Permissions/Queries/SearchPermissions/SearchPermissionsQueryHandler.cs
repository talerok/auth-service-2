using Auth.Application;
using Auth.Application.Permissions.Queries.SearchPermissions;
using MediatR;

namespace Auth.Infrastructure.Permissions.Queries.SearchPermissions;

internal sealed class SearchPermissionsQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchPermissionsQuery, SearchResponse<PermissionDto>>
{
    public Task<SearchResponse<PermissionDto>> Handle(SearchPermissionsQuery query, CancellationToken cancellationToken) =>
        searchService.SearchPermissionsAsync(query.Request, cancellationToken);
}

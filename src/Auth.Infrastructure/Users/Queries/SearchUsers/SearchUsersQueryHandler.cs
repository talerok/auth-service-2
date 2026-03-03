using Auth.Application;
using Auth.Application.Users.Queries.SearchUsers;
using MediatR;

namespace Auth.Infrastructure.Users.Queries.SearchUsers;

internal sealed class SearchUsersQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchUsersQuery, SearchResponse<UserDto>>
{
    public Task<SearchResponse<UserDto>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        return searchService.SearchUsersAsync(query.Request, cancellationToken);
    }
}

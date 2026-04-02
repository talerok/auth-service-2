using Auth.Application;
using Auth.Application.Sessions;
using Auth.Application.Sessions.Queries.SearchSessions;
using MediatR;

namespace Auth.Infrastructure.Sessions.Queries.SearchSessions;

internal sealed class SearchSessionsQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchSessionsQuery, SearchResponse<UserSessionSearchDto>>
{
    public Task<SearchResponse<UserSessionSearchDto>> Handle(SearchSessionsQuery query, CancellationToken cancellationToken) =>
        searchService.SearchSessionsAsync(query.Request, cancellationToken);
}

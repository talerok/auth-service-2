using Auth.Application;
using Auth.Application.Applications.Queries.SearchApplications;
using MediatR;

namespace Auth.Infrastructure.Applications.Queries.SearchApplications;

internal sealed class SearchApplicationsQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchApplicationsQuery, SearchResponse<ApplicationDto>>
{
    public Task<SearchResponse<ApplicationDto>> Handle(SearchApplicationsQuery query, CancellationToken cancellationToken)
    {
        return searchService.SearchApplicationsAsync(query.Request, cancellationToken);
    }
}

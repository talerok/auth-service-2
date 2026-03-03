using Auth.Application;
using Auth.Application.ApiClients.Queries.SearchApiClients;
using MediatR;

namespace Auth.Infrastructure.ApiClients.Queries.SearchApiClients;

internal sealed class SearchApiClientsQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchApiClientsQuery, SearchResponse<ApiClientDto>>
{
    public Task<SearchResponse<ApiClientDto>> Handle(SearchApiClientsQuery query, CancellationToken cancellationToken)
    {
        return searchService.SearchApiClientsAsync(query.Request, cancellationToken);
    }
}

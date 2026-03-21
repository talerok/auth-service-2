using Auth.Application;
using Auth.Application.ServiceAccounts.Queries.SearchServiceAccounts;
using MediatR;

namespace Auth.Infrastructure.ServiceAccounts.Queries.SearchServiceAccounts;

internal sealed class SearchServiceAccountsQueryHandler(
    ISearchService searchService) : IRequestHandler<SearchServiceAccountsQuery, SearchResponse<ServiceAccountDto>>
{
    public Task<SearchResponse<ServiceAccountDto>> Handle(SearchServiceAccountsQuery query, CancellationToken cancellationToken)
    {
        return searchService.SearchServiceAccountsAsync(query.Request, cancellationToken);
    }
}

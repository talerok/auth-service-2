using MediatR;

namespace Auth.Application.ServiceAccounts.Queries.SearchServiceAccounts;

public sealed record SearchServiceAccountsQuery(SearchRequest Request) : IRequest<SearchResponse<ServiceAccountDto>>;

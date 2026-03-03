using MediatR;

namespace Auth.Application.ApiClients.Queries.SearchApiClients;

public sealed record SearchApiClientsQuery(SearchRequest Request) : IRequest<SearchResponse<ApiClientDto>>;

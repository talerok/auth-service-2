using MediatR;

namespace Auth.Application.Applications.Queries.SearchApplications;

public sealed record SearchApplicationsQuery(SearchRequest Request) : IRequest<SearchResponse<ApplicationDto>>;

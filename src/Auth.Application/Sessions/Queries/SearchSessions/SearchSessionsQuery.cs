using MediatR;

namespace Auth.Application.Sessions.Queries.SearchSessions;

public sealed record SearchSessionsQuery(SearchRequest Request) : IRequest<SearchResponse<UserSessionSearchDto>>;

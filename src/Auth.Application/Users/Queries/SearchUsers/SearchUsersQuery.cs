using MediatR;

namespace Auth.Application.Users.Queries.SearchUsers;

public sealed record SearchUsersQuery(SearchRequest Request) : IRequest<SearchResponse<UserDto>>;

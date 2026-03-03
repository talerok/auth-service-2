using MediatR;

namespace Auth.Application.Roles.Queries.SearchRoles;

public sealed record SearchRolesQuery(SearchRequest Request) : IRequest<SearchResponse<RoleDto>>;

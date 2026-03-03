using MediatR;

namespace Auth.Application.Permissions.Queries.SearchPermissions;

public sealed record SearchPermissionsQuery(SearchRequest Request) : IRequest<SearchResponse<PermissionDto>>;

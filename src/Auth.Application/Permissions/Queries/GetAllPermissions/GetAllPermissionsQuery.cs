using MediatR;

namespace Auth.Application.Permissions.Queries.GetAllPermissions;

public sealed record GetAllPermissionsQuery() : IRequest<IReadOnlyCollection<PermissionDto>>;

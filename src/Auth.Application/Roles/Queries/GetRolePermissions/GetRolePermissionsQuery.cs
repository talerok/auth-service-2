using MediatR;

namespace Auth.Application.Roles.Queries.GetRolePermissions;

public sealed record GetRolePermissionsQuery(Guid RoleId) : IRequest<IReadOnlyCollection<PermissionDto>?>;
